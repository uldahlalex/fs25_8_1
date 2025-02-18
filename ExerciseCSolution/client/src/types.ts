import { BaseDto } from "ws-request-hook";

export interface Point {
    x: number;
    y: number;
}

export type DrawingTool = 'pencil' | 'circle' | 'square' | 'text' | 'eraser';

export interface DrawingAction {
    tool: DrawingTool;
    color: string;
    lineWidth: number;
    startPoint: Point;
    endPoint: Point;
}export type ClientWantsToDrawDto = BaseDto & {
    roomId: string;
    action: DrawingAction;
};export type ServerBroadcastsDrawingDto = BaseDto & {
    roomId: string;
    action: DrawingAction;
};

export type ServerConfirmsDrawDto = BaseDto & {

}

// Client -> Server DTOs
export type ClientWantsToJoinRoomDto = BaseDto & {
    roomId: string;
};

export type ClientWantsToLeaveRoomDto = BaseDto & {
    roomId: string;
};



// Server -> Client DTOs
export type ServerConfirmsJoinRoomDto = BaseDto & {
    roomId: string;
    success: boolean;
};



// Constants
export const StringConstants = {
    ClientWantsToJoinRoom: 'ClientWantsToJoinRoom',
    ClientWantsToLeaveRoom: 'ClientWantsToLeaveRoom',
    ClientWantsToDraw: 'ClientWantsToDraw',
    ServerConfirmsJoinRoom: 'ServerConfirmsJoinRoom',
    ServerBroadcastsDrawing: 'ServerBroadcastsDrawing',
    ServerConfirmsDraw: 'ServerConfirmsDraw'
} as const;