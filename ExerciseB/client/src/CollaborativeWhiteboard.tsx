import React, { useEffect, useRef, useState } from 'react';
import { useWsClient } from 'ws-request-hook';
import {
    DrawingTool,
    Point,
    DrawingAction,
    ClientWantsToJoinRoomDto,
    ClientWantsToLeaveRoomDto,
    ClientWantsToDrawDto,
    ServerConfirmsJoinRoomDto,
    ServerBroadcastsDrawingDto,
    StringConstants,
    ServerConfirmsDrawDto
} from './types';
import CanvasTools from './CanvasTools';

export default function CollaborativeWhiteboard() {
    const { sendRequest, onMessage, readyState } = useWsClient();
    const canvasRef = useRef<HTMLCanvasElement>(null);

    const [room, setRoom] = useState<string>('');
    const [isDrawing, setIsDrawing] = useState(false);
    const [tool, setTool] = useState<DrawingTool>('pencil');
    const [color, setColor] = useState('#000000');
    const [lineWidth, setLineWidth] = useState(2);
    const [startPoint, setStartPoint] = useState<Point | null>(null);
    const [lastPoint, setLastPoint] = useState<Point | null>(null);

    useEffect(() => {
        if (readyState !== 1) return;

        const unsubscribe = onMessage<ServerBroadcastsDrawingDto>(
            StringConstants.ServerBroadcastsDrawing,
            (message) => {
                if (message.roomId === room) {
                    drawFromServer(message.action);
                }
            }
        );

        return () => unsubscribe();
    }, [onMessage, readyState, room]);

    const joinRoom = async (roomId: string) => {
        if (room) {
            const leaveDto: ClientWantsToLeaveRoomDto = {
                eventType: StringConstants.ClientWantsToLeaveRoom,
                roomId: room
            };
            await sendRequest<ClientWantsToLeaveRoomDto, ServerConfirmsJoinRoomDto>(
                leaveDto,
                StringConstants.ServerConfirmsJoinRoom
            );
        }

        const joinDto: ClientWantsToJoinRoomDto = {
            eventType: StringConstants.ClientWantsToJoinRoom,
            roomId
        };

        try {
            const response = await sendRequest<ClientWantsToJoinRoomDto, ServerConfirmsJoinRoomDto>(
                joinDto,
                StringConstants.ServerConfirmsJoinRoom
            );

            if (response.success) {
                setRoom(roomId);
                clearCanvas();
            }
        } catch (error) {
            console.error('Failed to join room:', error);
        }
    };

    const clearCanvas = () => {
        const canvas = canvasRef.current;
        const ctx = canvas?.getContext('2d');
        if (ctx && canvas) {
            ctx.clearRect(0, 0, canvas.width, canvas.height);
        }
    };

    const drawFromServer = (action: DrawingAction) => {
        const canvas = canvasRef.current;
        const ctx = canvas?.getContext('2d');
        if (!ctx || !canvas) return;

        ctx.strokeStyle = action.color;
        ctx.lineWidth = action.lineWidth;
        ctx.lineCap = 'round';
        ctx.lineJoin = 'round';

        switch (action.tool.toLowerCase()) { // Add toLowerCase() to handle case differences
            case 'pencil':
                ctx.beginPath();
                ctx.moveTo(action.startPoint.x, action.startPoint.y);
                ctx.lineTo(action.endPoint.x, action.endPoint.y);
                ctx.stroke();
                break;
            case 'circle':
                ctx.beginPath();
                const radius = Math.sqrt(
                    Math.pow(action.endPoint.x - action.startPoint.x, 2) +
                    Math.pow(action.endPoint.y - action.startPoint.y, 2)
                );
                ctx.arc(action.startPoint.x, action.startPoint.y, radius, 0, 2 * Math.PI);
                ctx.stroke();
                break;
            case 'square':
                ctx.beginPath();
                ctx.strokeRect(
                    action.startPoint.x,
                    action.startPoint.y,
                    action.endPoint.x - action.startPoint.x,
                    action.endPoint.y - action.startPoint.y
                );
                break;
            case 'eraser':
                ctx.strokeStyle = '#FFFFFF';
                ctx.beginPath();
                ctx.moveTo(action.startPoint.x, action.startPoint.y);
                ctx.lineTo(action.endPoint.x, action.endPoint.y);
                ctx.stroke();
                break;
        }
    };

    const startDrawing = (e: React.MouseEvent<HTMLCanvasElement>) => {
        if (!room || !canvasRef.current) return;

        const rect = canvasRef.current.getBoundingClientRect();
        const point: Point = {
            x: e.clientX - rect.left,
            y: e.clientY - rect.top
        };

        setIsDrawing(true);
        setStartPoint(point);
        setLastPoint(point);
    };

    const draw = (e: React.MouseEvent<HTMLCanvasElement>) => {
        if (!isDrawing || !room || !lastPoint || !canvasRef.current) return;

        const rect = canvasRef.current.getBoundingClientRect();
        const currentPoint: Point = {
            x: e.clientX - rect.left,
            y: e.clientY - rect.top
        };

        // Only update the last point - no local drawing
        setLastPoint(currentPoint);
    };

    const stopDrawing = async () => {
        if (!isDrawing || !startPoint || !lastPoint) return;

        const action: DrawingAction = {
            tool,
            color,
            lineWidth,
            startPoint,
            endPoint: lastPoint
        };

        const drawDto: ClientWantsToDrawDto = {
            eventType: StringConstants.ClientWantsToDraw,
            roomId: room,
            action
        };

        try {
            await sendRequest<ClientWantsToDrawDto, ServerConfirmsDrawDto>(
                drawDto,
                StringConstants.ServerConfirmsDraw
            );
        } catch (error) {
            console.error('Failed to send drawing action:', error);
        }

        setIsDrawing(false);
        setStartPoint(null);
        setLastPoint(null);
    };

    return (
        <div className="flex flex-col h-screen bg-gray-100">
            <CanvasTools
                room={room}
                tool={tool}
                color={color}
                lineWidth={lineWidth}
                onRoomChange={joinRoom}
                onToolChange={setTool}
                onColorChange={setColor}
                onLineWidthChange={setLineWidth}
                onClearCanvas={clearCanvas}
            />

            <div className="flex-1 p-4">
                <canvas
                    ref={canvasRef}
                    width={800}
                    height={600}
                    className="w-full h-full bg-white rounded-lg shadow"
                    style={{ border: '1px solid black' }}
                    onMouseDown={startDrawing}
                    onMouseMove={draw}
                    onMouseUp={stopDrawing}
                    onMouseOut={stopDrawing}
                    onMouseLeave={stopDrawing}
                />
            </div>
        </div>
    );
}