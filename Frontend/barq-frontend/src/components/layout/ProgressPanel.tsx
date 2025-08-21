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
  Upload,
  DollarSign
} from 'lucide-react';
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from '../ui/dropdown-menu';
import { cn } from '../../lib/utils';
import { useFileWorkflow } from '../../hooks/useFileWorkflow';
import { useSLAWorkflow } from '../../hooks/useSLAWorkflow';
import { useBillingWorkflow } from '../../hooks/useBillingWorkflow';
import { toast } from 'sonner';

interface ProgressPanelProps {
  collapsed: boolean;
}

export function ProgressPanel({ collapsed }: ProgressPanelProps) {
  const [activeTab, setActiveTab] = useState('progress');
  const { state: fileState, uploadAndScanFile, downloadFile, deleteFile } = useFileWorkflow();
  const { state: slaState, acknowledgeViolation } = useSLAWorkflow();
  const { state: billingState, handle402Response } = useBillingWorkflow();

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
    <div className="h-full border-l bg-muted/30 flex flex-col">
      {/* Header */}
      <div className="p-4 border-b">
        <h2 className="font-semibold text-lg">Progress & Activity</h2>
      </div>

      {/* Tabs */}
      <Tabs value={activeTab} onValueChange={setActiveTab} className="flex-1 flex flex-col">
        <TabsList className="grid w-full grid-cols-4 mx-4 mt-2">
          <TabsTrigger value="progress" className="text-xs">Progress</TabsTrigger>
          <TabsTrigger value="documents" className="text-xs">Files</TabsTrigger>
          <TabsTrigger value="sla" className="text-xs">SLA</TabsTrigger>
          <TabsTrigger value="billing" className="text-xs">Billing</TabsTrigger>
        </TabsList>

        <div className="flex-1 overflow-hidden">
          <TabsContent value="progress" className="h-full m-0">
            <ProgressView />
          </TabsContent>
          
          <TabsContent value="documents" className="h-full m-0">
            <DocumentsView files={fileState.files} onDownload={downloadFile} onDelete={deleteFile} />
          </TabsContent>
          
          <TabsContent value="sla" className="h-full m-0">
            <SLAView violations={slaState.violations} onAcknowledge={acknowledgeViolation} />
          </TabsContent>
          
          <TabsContent value="billing" className="h-full m-0">
            <BillingView status={billingState.status} onUpgrade={handle402Response} />
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

interface DocumentsViewProps {
  files: any[];
  onDownload: (fileId: string) => void;
  onDelete: (fileId: string) => void;
}

function DocumentsView({ files, onDownload, onDelete }: DocumentsViewProps) {

  const getFileIcon = (type: string) => {
    return <FileText className="h-4 w-4 text-blue-500" />;
  };

  const getStatusColor = (status: string) => {
    switch (status) {
      case 'clean':
        return 'bg-green-100 text-green-800 dark:bg-green-900 dark:text-green-200';
      case 'pending':
        return 'bg-yellow-100 text-yellow-800 dark:bg-yellow-900 dark:text-yellow-200';
      case 'infected':
      case 'quarantined':
        return 'bg-red-100 text-red-800 dark:bg-red-900 dark:text-red-200';
      default:
        return 'bg-gray-100 text-gray-800 dark:bg-gray-900 dark:text-gray-200';
    }
  };

  return (
    <ScrollArea className="h-full">
      <div className="p-4 space-y-3">
        {files.map((doc) => (
          <div
            key={doc.id}
            className="p-3 rounded-lg border bg-card hover:bg-accent/50 transition-colors"
          >
            <div className="flex items-start justify-between">
              <div className="flex items-start gap-3 flex-1 min-w-0">
                {getFileIcon(doc.type)}
                <div className="flex-1 min-w-0">
                  <h4 className="font-medium text-sm truncate">{doc.fileName}</h4>
                  <div className="flex items-center gap-2 mt-1">
                    <Badge variant="secondary" className={cn("text-xs", getStatusColor(doc.scanStatus))}>
                      {doc.scanStatus}
                    </Badge>
                    <span className="text-xs text-muted-foreground">{(doc.fileSize / 1024 / 1024).toFixed(1)} MB</span>
                  </div>
                  <div className="text-xs text-muted-foreground mt-1">
                    Uploaded {new Date(doc.uploadedAt).toLocaleDateString()}
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
                  <DropdownMenuItem onClick={() => onDownload(doc.id)}>
                    <Download className="mr-2 h-4 w-4" />
                    Download
                  </DropdownMenuItem>
                  <DropdownMenuSeparator />
                  <DropdownMenuItem 
                    className="text-destructive"
                    onClick={() => onDelete(doc.id)}
                  >
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

interface SLAViewProps {
  violations: any[];
  onAcknowledge: (violationId: string) => void;
}

function SLAView({ violations, onAcknowledge }: SLAViewProps) {
  const getSeverityColor = (severity: string) => {
    switch (severity) {
      case 'critical':
        return 'bg-red-100 text-red-800 dark:bg-red-900 dark:text-red-200';
      case 'high':
        return 'bg-orange-100 text-orange-800 dark:bg-orange-900 dark:text-orange-200';
      case 'medium':
        return 'bg-yellow-100 text-yellow-800 dark:bg-yellow-900 dark:text-yellow-200';
      default:
        return 'bg-gray-100 text-gray-800 dark:bg-gray-900 dark:text-gray-200';
    }
  };

  return (
    <ScrollArea className="h-full">
      <div className="p-4 space-y-3">
        {violations.length === 0 ? (
          <div className="text-center py-8 text-muted-foreground">
            <CheckCircle2 className="h-8 w-8 mx-auto mb-2 text-green-500" />
            <p>No SLA violations</p>
          </div>
        ) : (
          violations.map((violation) => (
            <div
              key={violation.id}
              className="p-3 rounded-lg border bg-card"
            >
              <div className="flex items-start justify-between">
                <div className="flex-1 min-w-0">
                  <div className="flex items-center gap-2 mb-2">
                    <Badge variant="secondary" className={cn("text-xs", getSeverityColor(violation.severity))}>
                      {violation.severity}
                    </Badge>
                    <span className="text-xs text-muted-foreground">
                      {violation.type}
                    </span>
                  </div>
                  <h4 className="font-medium text-sm">{violation.taskTitle}</h4>
                  <p className="text-xs text-muted-foreground mt-1">
                    Violated at {new Date(violation.violatedAt).toLocaleString()}
                  </p>
                  <p className="text-xs text-muted-foreground">
                    Overdue by {violation.overdueMinutes} minutes
                  </p>
                </div>
                
                {!violation.acknowledged && (
                  <Button 
                    size="sm" 
                    variant="outline"
                    onClick={() => onAcknowledge(violation.id)}
                  >
                    Acknowledge
                  </Button>
                )}
              </div>
            </div>
          ))
        )}
      </div>
    </ScrollArea>
  );
}

interface BillingViewProps {
  status?: any;
  onUpgrade: () => void;
}

function BillingView({ status, onUpgrade }: BillingViewProps) {
  if (!status) {
    return (
      <ScrollArea className="h-full">
        <div className="p-4 text-center text-muted-foreground">
          Loading billing information...
        </div>
      </ScrollArea>
    );
  }

  const usagePercentage = (status.currentUsage / status.planLimit) * 100;

  return (
    <ScrollArea className="h-full">
      <div className="p-4 space-y-4">
        <div className="space-y-3">
          <div className="flex items-center justify-between">
            <h3 className="font-medium">Usage Overview</h3>
            <Badge variant={status.isOverQuota ? "destructive" : "secondary"}>
              {status.isOverQuota ? "Over Quota" : "Within Limits"}
            </Badge>
          </div>
          
          <div className="space-y-2">
            <div className="flex items-center justify-between text-sm">
              <span>Current Usage</span>
              <span>{status.currentUsage} / {status.planLimit}</span>
            </div>
            <div className="w-full bg-secondary rounded-full h-2">
              <div 
                className={cn(
                  "h-2 rounded-full transition-all",
                  status.isOverQuota ? "bg-red-500" : "bg-primary"
                )}
                style={{ width: `${Math.min(usagePercentage, 100)}%` }}
              />
            </div>
            <div className="text-xs text-muted-foreground">
              {usagePercentage.toFixed(1)}% of plan limit used
            </div>
          </div>
        </div>

        {status.isOverQuota && (
          <div className="p-3 rounded-lg border border-red-200 bg-red-50 dark:border-red-800 dark:bg-red-950">
            <div className="flex items-center gap-2 mb-2">
              <AlertCircle className="h-4 w-4 text-red-500" />
              <span className="font-medium text-sm text-red-700 dark:text-red-300">
                Quota Exceeded
              </span>
            </div>
            <p className="text-xs text-red-600 dark:text-red-400 mb-3">
              Your usage has exceeded the current plan limits. Upgrade to continue using the service.
            </p>
            <Button size="sm" onClick={onUpgrade} className="w-full">
              <DollarSign className="mr-2 h-4 w-4" />
              Upgrade Plan
            </Button>
          </div>
        )}

        <div className="grid grid-cols-1 gap-3">
          <div className="p-3 rounded-lg border bg-card">
            <div className="flex items-center gap-2">
              <Activity className="h-4 w-4 text-blue-500" />
              <span className="text-xs font-medium">API Calls</span>
            </div>
            <div className="text-lg font-semibold mt-1">{status.currentUsage}</div>
            <div className="text-xs text-muted-foreground">This month</div>
          </div>
        </div>
      </div>
    </ScrollArea>
  );
}
