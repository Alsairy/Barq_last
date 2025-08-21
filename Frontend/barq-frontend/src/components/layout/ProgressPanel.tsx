import React, { useState } from 'react';
import { Button } from '../ui/button';
import { Badge } from '../ui/badge';
import { ScrollArea } from '../ui/scroll-area';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '../ui/tabs';
import { 
  Activity, 
  FileText, 
  Clock, 
  CheckCircle2, 
  AlertCircle, 
  Download, 
  Eye, 
  MoreHorizontal,
  TrendingUp,
  Calendar,
  Users,
  Target,
  RefreshCw
} from 'lucide-react';
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from '../ui/dropdown-menu';
import { cn } from '../../lib/utils';
import { useAutoRetry } from '../../hooks/useAutoRetry';
import { useI18n } from '../../hooks/useI18n';

interface ProgressPanelProps {
  collapsed: boolean;
}

export function ProgressPanel({ collapsed }: ProgressPanelProps) {
  const [activeTab, setActiveTab] = useState('progress');
  const { t, isRTL } = useI18n();

  const { data: progressData, isLoading, error, retry } = useAutoRetry(
    async () => {
      await new Promise(resolve => setTimeout(resolve, 500));
      return { progress: 75, status: 'running' };
    },
    {
      maxRetries: 3,
      retryDelay: 2000,
      exponentialBackoff: true
    }
  );

  if (collapsed) {
    return (
      <div className="h-full w-12 border-l bg-muted/30 flex flex-col items-center py-4 gap-2">
        <Button variant="ghost" size="sm" className="h-8 w-8 p-0">
          <Activity className="h-4 w-4" />
        </Button>
        <Button variant="ghost" size="sm" className="h-8 w-8 p-0">
          <FileText className="h-4 w-4" />
        </Button>
        <Button variant="ghost" size="sm" className="h-8 w-8 p-0">
          <TrendingUp className="h-4 w-4" />
        </Button>
      </div>
    );
  }

  return (
    <div className={cn("h-full border-l bg-muted/30 flex flex-col", isRTL && "border-r border-l-0")}>
      {/* Header */}
      <div className="p-4 border-b">
        <div className="flex items-center justify-between">
          <h2 className="font-semibold text-lg">{t('progress_and_activity', 'Progress & Activity')}</h2>
          {error && (
            <Button variant="ghost" size="sm" onClick={retry} className="h-8 w-8 p-0">
              <RefreshCw className="h-4 w-4" />
            </Button>
          )}
        </div>
      </div>

      {/* Tabs */}
      <Tabs value={activeTab} onValueChange={setActiveTab} className="flex-1 flex flex-col">
        <TabsList className="grid w-full grid-cols-3 mx-4 mt-2">
          <TabsTrigger value="progress" className="text-xs">Progress</TabsTrigger>
          <TabsTrigger value="documents" className="text-xs">Documents</TabsTrigger>
          <TabsTrigger value="activity" className="text-xs">Activity</TabsTrigger>
        </TabsList>

        <div className="flex-1 overflow-hidden">
          <TabsContent value="progress" className="h-full m-0">
            <ProgressView />
          </TabsContent>
          
          <TabsContent value="documents" className="h-full m-0">
            <DocumentsView />
          </TabsContent>
          
          <TabsContent value="activity" className="h-full m-0">
            <ActivityView />
          </TabsContent>
        </div>
      </Tabs>
    </div>
  );
}

function ProgressView() {
  const workflowProgress = {
    currentStep: 'Data Processing',
    totalSteps: 5,
    completedSteps: 2,
    progress: 40,
    estimatedCompletion: '2 hours',
    status: 'running'
  };

  const milestones = [
    {
      id: '1',
      name: 'Requirements Gathering',
      status: 'completed',
      completedAt: '2024-01-10T10:00:00Z',
      duration: '2 days'
    },
    {
      id: '2',
      name: 'Design Phase',
      status: 'completed',
      completedAt: '2024-01-12T15:30:00Z',
      duration: '3 days'
    },
    {
      id: '3',
      name: 'Development',
      status: 'in-progress',
      progress: 65,
      estimatedCompletion: '2024-01-20T17:00:00Z'
    },
    {
      id: '4',
      name: 'Testing',
      status: 'pending',
      estimatedStart: '2024-01-21T09:00:00Z'
    },
    {
      id: '5',
      name: 'Deployment',
      status: 'pending',
      estimatedStart: '2024-01-25T14:00:00Z'
    }
  ];

  const getStatusIcon = (status: string) => {
    switch (status) {
      case 'completed':
        return <CheckCircle2 className="h-4 w-4 text-green-500" />;
      case 'in-progress':
        return <Clock className="h-4 w-4 text-blue-500" />;
      case 'pending':
        return <AlertCircle className="h-4 w-4 text-gray-400" />;
      default:
        return <Clock className="h-4 w-4 text-muted-foreground" />;
    }
  };

  return (
    <ScrollArea className="h-full">
      <div className="p-4 space-y-6">
        {/* Overall Progress */}
        <div className="space-y-3">
          <div className="flex items-center justify-between">
            <h3 className="font-medium">Overall Progress</h3>
            <Badge variant="outline" className={cn(
              workflowProgress.status === 'running' && "border-blue-500 text-blue-700",
              workflowProgress.status === 'completed' && "border-green-500 text-green-700",
              workflowProgress.status === 'failed' && "border-red-500 text-red-700"
            )}>
              {workflowProgress.status}
            </Badge>
          </div>
          
          <div className="space-y-2">
            <div className="flex items-center justify-between text-sm">
              <span>Step {workflowProgress.completedSteps} of {workflowProgress.totalSteps}</span>
              <span>{workflowProgress.progress}%</span>
            </div>
            <div className="w-full bg-secondary rounded-full h-2">
              <div 
                className="bg-primary h-2 rounded-full transition-all"
                style={{ width: `${workflowProgress.progress}%` }}
              />
            </div>
            <div className="text-xs text-muted-foreground">
              Current: {workflowProgress.currentStep}
            </div>
            <div className="text-xs text-muted-foreground">
              ETA: {workflowProgress.estimatedCompletion}
            </div>
          </div>
        </div>

        {/* Milestones */}
        <div className="space-y-3">
          <h3 className="font-medium">Milestones</h3>
          <div className="space-y-3">
            {milestones.map((milestone, index) => (
              <div key={milestone.id} className="flex items-start gap-3">
                <div className="flex flex-col items-center">
                  {getStatusIcon(milestone.status)}
                  {index < milestones.length - 1 && (
                    <div className="w-px h-8 bg-border mt-2" />
                  )}
                </div>
                
                <div className="flex-1 min-w-0">
                  <div className="flex items-center justify-between">
                    <h4 className="font-medium text-sm">{milestone.name}</h4>
                    <Badge variant="secondary" className="text-xs">
                      {milestone.status}
                    </Badge>
                  </div>
                  
                  {milestone.status === 'completed' && (
                    <div className="text-xs text-muted-foreground mt-1">
                      Completed {new Date(milestone.completedAt!).toLocaleDateString()} • {milestone.duration}
                    </div>
                  )}
                  
                  {milestone.status === 'in-progress' && (
                    <div className="mt-2">
                      <div className="flex items-center justify-between text-xs mb-1">
                        <span>Progress</span>
                        <span>{milestone.progress}%</span>
                      </div>
                      <div className="w-full bg-secondary rounded-full h-1">
                        <div 
                          className="bg-blue-500 h-1 rounded-full"
                          style={{ width: `${milestone.progress}%` }}
                        />
                      </div>
                      <div className="text-xs text-muted-foreground mt-1">
                        ETA: {new Date(milestone.estimatedCompletion!).toLocaleDateString()}
                      </div>
                    </div>
                  )}
                  
                  {milestone.status === 'pending' && (
                    <div className="text-xs text-muted-foreground mt-1">
                      Starts {new Date(milestone.estimatedStart!).toLocaleDateString()}
                    </div>
                  )}
                </div>
              </div>
            ))}
          </div>
        </div>

        {/* Quick Stats */}
        <div className="grid grid-cols-2 gap-3">
          <div className="p-3 rounded-lg border bg-card">
            <div className="flex items-center gap-2">
              <Target className="h-4 w-4 text-blue-500" />
              <span className="text-xs font-medium">Tasks</span>
            </div>
            <div className="text-lg font-semibold mt-1">12/18</div>
            <div className="text-xs text-muted-foreground">Completed</div>
          </div>
          
          <div className="p-3 rounded-lg border bg-card">
            <div className="flex items-center gap-2">
              <Users className="h-4 w-4 text-green-500" />
              <span className="text-xs font-medium">Team</span>
            </div>
            <div className="text-lg font-semibold mt-1">8</div>
            <div className="text-xs text-muted-foreground">Members</div>
          </div>
        </div>
      </div>
    </ScrollArea>
  );
}

function DocumentsView() {
  const documents = [
    {
      id: '1',
      name: 'Project Requirements.pdf',
      type: 'pdf',
      size: '2.4 MB',
      uploadedAt: '2024-01-10T10:00:00Z',
      uploadedBy: 'John Doe',
      status: 'approved'
    },
    {
      id: '2',
      name: 'Design Mockups.figma',
      type: 'figma',
      size: '15.2 MB',
      uploadedAt: '2024-01-12T14:30:00Z',
      uploadedBy: 'Jane Smith',
      status: 'review'
    },
    {
      id: '3',
      name: 'API Documentation.md',
      type: 'markdown',
      size: '156 KB',
      uploadedAt: '2024-01-15T09:15:00Z',
      uploadedBy: 'Mike Johnson',
      status: 'draft'
    },
    {
      id: '4',
      name: 'Test Results.xlsx',
      type: 'excel',
      size: '892 KB',
      uploadedAt: '2024-01-16T16:45:00Z',
      uploadedBy: 'Sarah Wilson',
      status: 'approved'
    }
  ];

  const getFileIcon = (type: string) => {
    return <FileText className="h-4 w-4 text-blue-500" />;
  };

  const getStatusColor = (status: string) => {
    switch (status) {
      case 'approved':
        return 'bg-green-100 text-green-800 dark:bg-green-900 dark:text-green-200';
      case 'review':
        return 'bg-yellow-100 text-yellow-800 dark:bg-yellow-900 dark:text-yellow-200';
      case 'draft':
        return 'bg-gray-100 text-gray-800 dark:bg-gray-900 dark:text-gray-200';
      default:
        return 'bg-gray-100 text-gray-800 dark:bg-gray-900 dark:text-gray-200';
    }
  };

  return (
    <ScrollArea className="h-full">
      <div className="p-4 space-y-3">
        {documents.map((doc) => (
          <div
            key={doc.id}
            className="p-3 rounded-lg border bg-card hover:bg-accent/50 transition-colors"
          >
            <div className="flex items-start justify-between">
              <div className="flex items-start gap-3 flex-1 min-w-0">
                {getFileIcon(doc.type)}
                <div className="flex-1 min-w-0">
                  <h4 className="font-medium text-sm truncate">{doc.name}</h4>
                  <div className="flex items-center gap-2 mt-1">
                    <Badge variant="secondary" className={cn("text-xs", getStatusColor(doc.status))}>
                      {doc.status}
                    </Badge>
                    <span className="text-xs text-muted-foreground">{doc.size}</span>
                  </div>
                  <div className="text-xs text-muted-foreground mt-1">
                    By {doc.uploadedBy} • {new Date(doc.uploadedAt).toLocaleDateString()}
                  </div>
                </div>
              </div>
              
              <DropdownMenu>
                <DropdownMenuTrigger asChild>
                  <Button variant="ghost" size="sm" className="h-6 w-6 p-0">
                    <MoreHorizontal className="h-3 w-3" />
                  </Button>
                </DropdownMenuTrigger>
                <DropdownMenuContent align="end">
                  <DropdownMenuItem>
                    <Eye className="mr-2 h-4 w-4" />
                    View
                  </DropdownMenuItem>
                  <DropdownMenuItem>
                    <Download className="mr-2 h-4 w-4" />
                    Download
                  </DropdownMenuItem>
                  <DropdownMenuSeparator />
                  <DropdownMenuItem className="text-destructive">
                    Delete
                  </DropdownMenuItem>
                </DropdownMenuContent>
              </DropdownMenu>
            </div>
          </div>
        ))}
      </div>
    </ScrollArea>
  );
}

function ActivityView() {
  const activities = [
    {
      id: '1',
      type: 'task_completed',
      message: 'Task "Implement user authentication" was completed',
      user: 'John Doe',
      timestamp: '2024-01-16T16:30:00Z'
    },
    {
      id: '2',
      type: 'document_uploaded',
      message: 'Document "Test Results.xlsx" was uploaded',
      user: 'Sarah Wilson',
      timestamp: '2024-01-16T16:45:00Z'
    },
    {
      id: '3',
      type: 'workflow_started',
      message: 'Workflow "Code Review Process" was started',
      user: 'Mike Johnson',
      timestamp: '2024-01-16T15:20:00Z'
    },
    {
      id: '4',
      type: 'comment_added',
      message: 'Comment added to task "Setup CI/CD pipeline"',
      user: 'Jane Smith',
      timestamp: '2024-01-16T14:15:00Z'
    },
    {
      id: '5',
      type: 'milestone_reached',
      message: 'Milestone "Design Phase" was completed',
      user: 'System',
      timestamp: '2024-01-16T12:00:00Z'
    }
  ];

  const getActivityIcon = (type: string) => {
    switch (type) {
      case 'task_completed':
        return <CheckCircle2 className="h-4 w-4 text-green-500" />;
      case 'document_uploaded':
        return <FileText className="h-4 w-4 text-blue-500" />;
      case 'workflow_started':
        return <Activity className="h-4 w-4 text-purple-500" />;
      case 'comment_added':
        return <Users className="h-4 w-4 text-orange-500" />;
      case 'milestone_reached':
        return <Target className="h-4 w-4 text-green-500" />;
      default:
        return <Activity className="h-4 w-4 text-muted-foreground" />;
    }
  };

  return (
    <ScrollArea className="h-full">
      <div className="p-4 space-y-4">
        {activities.map((activity, index) => (
          <div key={activity.id} className="flex items-start gap-3">
            <div className="flex flex-col items-center">
              {getActivityIcon(activity.type)}
              {index < activities.length - 1 && (
                <div className="w-px h-8 bg-border mt-2" />
              )}
            </div>
            
            <div className="flex-1 min-w-0">
              <p className="text-sm">{activity.message}</p>
              <div className="flex items-center gap-2 mt-1 text-xs text-muted-foreground">
                <span>{activity.user}</span>
                <span>•</span>
                <span>{new Date(activity.timestamp).toLocaleString()}</span>
              </div>
            </div>
          </div>
        ))}
      </div>
    </ScrollArea>
  );
}
