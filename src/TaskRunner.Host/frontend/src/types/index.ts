// Task Types
export interface TaskTypeInfo {
    taskType: string;
    displayName: string;
}

export interface TaskProgress {
    percentComplete: number | null;
    currentPhase: string | null;
    statusMessage: string | null;
    currentItem: string | null;
    totalItems: number | null;
    processedItems: number | null;
    estimatedSecondsRemaining: number | null;
    customData: Record<string, unknown> | null;
}

export interface TaskResult {
    success: boolean;
    message: string | null;
    error: string | null;
    data: Record<string, unknown> | null;
}

export interface RunningTaskStatus {
    taskId: string;
    taskType: string;
    startTime: string;
}

export interface TaskState {
    taskId: string;
    taskType: string;
    status: 'running' | 'completed' | 'cancelled' | 'failed';
    progress: TaskProgress | null;
    result: TaskResult | null;
    startTime?: string;
    endTime?: string;
}

// Operation Types
export interface OperationTypeInfo {
    operationType: string;
    displayName: string;
}

export interface OperationResult {
    success: boolean;
    message: string | null;
    error: string | null;
    data: Record<string, unknown> | null;
}

// API Request/Response Types
export interface StartTaskRequest {
    taskType: string;
    parameters: Record<string, unknown>;
}

export interface StartTaskResponse {
    taskId: string;
}

export interface ExecuteOperationRequest {
    operationType: string;
    parameters: Record<string, unknown>;
}

// SignalR Event Types
export interface TaskStartedEvent {
    taskId: string;
    taskType: string;
    startTime: string;
}

export interface TaskProgressEvent {
    taskId: string;
    progress: TaskProgress;
}

export interface TaskCompletedEvent {
    taskId: string;
    result: TaskResult;
    endTime: string;
}

export interface TaskCancelledEvent {
    taskId: string;
    endTime: string;
}

export interface TaskFailedEvent {
    taskId: string;
    error: string;
    endTime: string;
}
