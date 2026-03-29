export interface ToolDefinition {
  id: string;
  name: string;
  description: string;
  icon: string;
}

export const tools: ToolDefinition[] = [
  {
    id: 'directory-size',
    name: 'Directory Size',
    description: 'Analyze folder sizes and find the largest directories',
    icon: 'directory-size',
  },
  {
    id: 'find-files',
    name: 'Find Files',
    description: 'Find files by filter across directories',
    icon: 'find-files',
  }
];
