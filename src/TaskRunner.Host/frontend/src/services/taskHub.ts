import * as signalR from '@microsoft/signalr';
import type {
    TaskStartedEvent,
    TaskProgressEvent,
    TaskCompletedEvent,
    TaskCancelledEvent,
    TaskFailedEvent
} from '../types';

const HUB_URL = '/hubs/tasks';

type TaskEventType = 'TaskStarted' | 'TaskProgress' | 'TaskCompleted' | 'TaskCancelled' | 'TaskFailed';
type TaskEventData = TaskStartedEvent | TaskProgressEvent | TaskCompletedEvent | TaskCancelledEvent | TaskFailedEvent;
type EventCallback<T> = (data: T) => void;

class TaskHubService {
    private connection: signalR.HubConnection | null = null;
    private listeners: Map<TaskEventType, Set<EventCallback<TaskEventData>>> = new Map();

    async connect(): Promise<void> {
        if (this.connection?.state === signalR.HubConnectionState.Connected) {
            return;
        }

        this.connection = new signalR.HubConnectionBuilder()
            .withUrl(HUB_URL)
            .withAutomaticReconnect()
            .configureLogging(signalR.LogLevel.Information)
            .build();

        // Set up event handlers
        this.connection.on('TaskStarted', (data: TaskStartedEvent) => this.emit('TaskStarted', data));
        this.connection.on('TaskProgress', (data: TaskProgressEvent) => this.emit('TaskProgress', data));
        this.connection.on('TaskCompleted', (data: TaskCompletedEvent) => this.emit('TaskCompleted', data));
        this.connection.on('TaskCancelled', (data: TaskCancelledEvent) => this.emit('TaskCancelled', data));
        this.connection.on('TaskFailed', (data: TaskFailedEvent) => this.emit('TaskFailed', data));

        await this.connection.start();
        console.log('SignalR Connected');
    }

    async subscribeToTask(taskId: string): Promise<void> {
        if (this.connection?.state === signalR.HubConnectionState.Connected) {
            await this.connection.invoke('SubscribeToTask', taskId);
        }
    }

    async unsubscribeFromTask(taskId: string): Promise<void> {
        if (this.connection?.state === signalR.HubConnectionState.Connected) {
            await this.connection.invoke('UnsubscribeFromTask', taskId);
        }
    }

    on<T extends TaskEventData>(event: TaskEventType, callback: EventCallback<T>): void {
        if (!this.listeners.has(event)) {
            this.listeners.set(event, new Set());
        }
        this.listeners.get(event)!.add(callback as EventCallback<TaskEventData>);
    }

    off<T extends TaskEventData>(event: TaskEventType, callback: EventCallback<T>): void {
        if (this.listeners.has(event)) {
            this.listeners.get(event)!.delete(callback as EventCallback<TaskEventData>);
        }
    }

    private emit(event: TaskEventType, data: TaskEventData): void {
        if (this.listeners.has(event)) {
            for (const callback of this.listeners.get(event)!) {
                callback(data);
            }
        }
    }

    async disconnect(): Promise<void> {
        if (this.connection) {
            await this.connection.stop();
        }
    }
}

export const taskHub = new TaskHubService();
