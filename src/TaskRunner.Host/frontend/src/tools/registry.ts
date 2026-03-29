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
];
