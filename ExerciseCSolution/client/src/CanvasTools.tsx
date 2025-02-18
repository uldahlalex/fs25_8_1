// CanvasTools.tsx
import { Menu } from '@headlessui/react';
import { Circle, Square, Type, Pencil, Eraser } from 'lucide-react';
import { DrawingTool } from './types';

interface CanvasToolsProps {
    room: string;
    tool: DrawingTool;
    color: string;
    lineWidth: number;
    onRoomChange: (room: string) => void;
    onToolChange: (tool: DrawingTool) => void;
    onColorChange: (color: string) => void;
    onLineWidthChange: (width: number) => void;
    onClearCanvas: () => void;
}

const tools: Array<{ icon: typeof Pencil; name: string; id: DrawingTool }> = [
    { icon: Pencil, name: 'Pencil', id: 'pencil' },
    { icon: Circle, name: 'Circle', id: 'circle' },
    { icon: Square, name: 'Square', id: 'square' },
    { icon: Type, name: 'Text', id: 'text' },
    { icon: Eraser, name: 'Eraser', id: 'eraser' },
];

const colors = [
    '#000000', '#FF0000', '#00FF00', '#0000FF',
    '#FFFF00', '#FF00FF', '#00FFFF', '#808080'
] as const;

const rooms = ['room1', 'room2', 'room3'] as const;

export default function CanvasTools({
                                        room,
                                        tool,
                                        color,
                                        lineWidth,
                                        onRoomChange,
                                        onToolChange,
                                        onColorChange,
                                        onLineWidthChange,
                                        onClearCanvas
                                    }: CanvasToolsProps) {
    return (
        <div className="flex items-center justify-between p-4 bg-white shadow">
            <div className="flex items-center space-x-4">
                <Menu as="div" className="relative">
                    <Menu.Button className="px-4 py-2 text-sm font-medium text-white bg-blue-600 rounded-md hover:bg-blue-700">
                        {room || 'Select Room'}
                    </Menu.Button>
                    <Menu.Items className="absolute z-10 mt-2 bg-white rounded-md shadow-lg">
                        {rooms.map((r) => (
                            <Menu.Item key={r}>
                                <button
                                    className={`block w-full px-4 py-2 text-sm text-left hover:bg-blue-50 ${
                                        room === r ? 'bg-blue-100' : ''
                                    }`}
                                    onClick={() => onRoomChange(r)}
                                >
                                    {r}
                                </button>
                            </Menu.Item>
                        ))}
                    </Menu.Items>
                </Menu>

                <div className="flex items-center space-x-2">
                    {tools.map(({ icon: Icon, name, id }) => (
                        <button
                            key={id}
                            className={`p-2 rounded-md hover:bg-gray-100 ${
                                tool === id ? 'bg-gray-200' : ''
                            }`}
                            onClick={() => onToolChange(id)}
                            title={name}
                        >
                            <Icon size={20} />
                        </button>
                    ))}
                </div>

                <div className="flex items-center space-x-2">
                    {colors.map((c) => (
                        <button
                            key={c}
                            className={`w-6 h-6 rounded-full border-2 ${
                                color === c ? 'border-gray-400' : 'border-transparent'
                            }`}
                            style={{ backgroundColor: c }}
                            onClick={() => onColorChange(c)}
                        />
                    ))}
                </div>

                <input
                    type="range"
                    min="1"
                    max="20"
                    value={lineWidth}
                    onChange={(e) => onLineWidthChange(parseInt(e.target.value))}
                    className="w-32"
                />
            </div>

            <button
                onClick={onClearCanvas}
                className="px-4 py-2 text-sm font-medium text-white bg-red-600 rounded-md hover:bg-red-700"
            >
                Clear Canvas
            </button>
        </div>
    );
}