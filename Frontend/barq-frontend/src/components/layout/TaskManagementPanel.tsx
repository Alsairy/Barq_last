import React, { useState } from 'react';
import { Button } from '../ui/button';
import { Input } from '../ui/input';
import { Badge } from '../ui/badge';
import { ScrollArea } from '../ui/scroll-area';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '../ui/tabs';
import { 
  Search, 
  Filter, 
  Plus, 
  MoreHorizontal, 
  CheckCircle2, 
  Clock, 
  AlertCircle,
  Users,
  FolderOpen,
  ListTodo
} from 'lucide-react';
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from '../ui/dropdown-menu';
import { cn } from '../../lib/utils';
import { useKeyboardNavigation } from '../../hooks/useKeyboardNavigation';
import { useI18n } from '../../hooks/useI18n';

interface TaskManagementPanelProps {
  collapsed: boolean;
}

export function TaskManagementPanel({ collapsed }: TaskManagementPanelProps) {
  const [searchQuery, setSearchQuery] = useState('');
  const [activeTab, setActiveTab] = useState('tasks');
  const [selectedIndex, setSelectedIndex] = useState(0);
  const { t, isRTL } = useI18n();

  useKeyboardNavigation({
    onArrowUp: () => setSelectedIndex(prev => Math.max(0, prev - 1)),
    onArrowDown: () => setSelectedIndex(prev => prev + 1),
    onEnter: () => {
    },
    enabled: !collapsed
  });

  if (collapsed) {
    return (
      <div className="h-full w-12 border-r bg-muted/30 flex flex-col items-center py-4 gap-2">
        <Button variant="ghost" size="sm" className="h-8 w-8 p-0">
          <ListTodo className="h-4 w-4" />
        </Button>
        <Button variant="ghost" size="sm" className="h-8 w-8 p-0">
          <FolderOpen className="h-4 w-4" />
        </Button>
        <Button variant="ghost" size="sm" className="h-8 w-8 p-0">
          <Users className="h-4 w-4" />
        </Button>
      </div>
    );
  }

  return (
    <div className={cn("h-full border-r bg-muted/30 flex flex-col", isRTL && "border-l border-r-0")}>
      {/* Header */}
      <div className="p-4 border-b">
        <div className="flex items-center justify-between mb-3">
          <h2 className="font-semibold text-lg">{t('tasks_and_projects', 'Tasks & Projects')}</h2>
          <Button size="sm" className="h-8">
            <Plus className={cn("h-4 w-4", isRTL ? "ml-1" : "mr-1")} />
            {t('new', 'New')}
          </Button>
        </div>
        
        {/* Search */}
        <div className="relative">
          <Search className={cn(
            "absolute top-1/2 transform -translate-y-1/2 h-4 w-4 text-muted-foreground",
            isRTL ? "right-3" : "left-3"
          )} />
          <Input
            placeholder={t('search_tasks', 'Search tasks...')}
            value={searchQuery}
            onChange={(e) => setSearchQuery(e.target.value)}
            className={cn("h-9", isRTL ? "pr-9" : "pl-9")}
            dir={isRTL ? "rtl" : "ltr"}
          />
        </div>
      </div>

      {/* Tabs */}
      <Tabs value={activeTab} onValueChange={setActiveTab} className="flex-1 flex flex-col">
        <TabsList className="grid w-full grid-cols-3 mx-4 mt-2">
          <TabsTrigger value="tasks" className="text-xs">Tasks</TabsTrigger>
          <TabsTrigger value="projects" className="text-xs">Projects</TabsTrigger>
          <TabsTrigger value="teams" className="text-xs">Teams</TabsTrigger>
        </TabsList>

        <div className="flex-1 overflow-hidden">
          <TabsContent value="tasks" className="h-full m-0">
            <TasksList searchQuery={searchQuery} />
          </TabsContent>
          
          <TabsContent value="projects" className="h-full m-0">
            <ProjectsList searchQuery={searchQuery} />
          </TabsContent>
          
          <TabsContent value="teams" className="h-full m-0">
            <TeamsList searchQuery={searchQuery} />
          </TabsContent>
        </div>
      </Tabs>
    </div>
  );
}

function TasksList({ searchQuery }: { searchQuery: string }) {
  const tasks = [
    {
      id: '1',
      title: 'Implement user authentication',
      status: 'in-progress',
      priority: 'high',
      assignee: 'John Doe',
      dueDate: '2024-01-15',
      project: 'BARQ Platform'
    },
    {
      id: '2',
      title: 'Design API endpoints',
      status: 'completed',
      priority: 'medium',
      assignee: 'Jane Smith',
      dueDate: '2024-01-10',
      project: 'BARQ Platform'
    },
    {
      id: '3',
      title: 'Setup CI/CD pipeline',
      status: 'pending',
      priority: 'high',
      assignee: 'Mike Johnson',
      dueDate: '2024-01-20',
      project: 'DevOps'
    }
  ];

  const filteredTasks = tasks.filter(task =>
    task.title.toLowerCase().includes(searchQuery.toLowerCase()) ||
    task.project.toLowerCase().includes(searchQuery.toLowerCase())
  );

  const getStatusIcon = (status: string) => {
    switch (status) {
      case 'completed':
        return <CheckCircle2 className="h-4 w-4 text-green-500" />;
      case 'in-progress':
        return <Clock className="h-4 w-4 text-blue-500" />;
      case 'pending':
        return <AlertCircle className="h-4 w-4 text-orange-500" />;
      default:
        return <Clock className="h-4 w-4 text-muted-foreground" />;
    }
  };

  const getPriorityColor = (priority: string) => {
    switch (priority) {
      case 'high':
        return 'bg-red-100 text-red-800 dark:bg-red-900 dark:text-red-200';
      case 'medium':
        return 'bg-yellow-100 text-yellow-800 dark:bg-yellow-900 dark:text-yellow-200';
      case 'low':
        return 'bg-green-100 text-green-800 dark:bg-green-900 dark:text-green-200';
      default:
        return 'bg-gray-100 text-gray-800 dark:bg-gray-900 dark:text-gray-200';
    }
  };

  return (
    <ScrollArea className="h-full">
      <div className="p-4 space-y-2">
        {filteredTasks.map((task) => (
          <div
            key={task.id}
            className="p-3 rounded-lg border bg-card hover:bg-accent/50 transition-colors cursor-pointer group"
          >
            <div className="flex items-start justify-between">
              <div className="flex-1 min-w-0">
                <div className="flex items-center gap-2 mb-1">
                  {getStatusIcon(task.status)}
                  <h4 className="font-medium text-sm truncate">{task.title}</h4>
                </div>
                
                <div className="flex items-center gap-2 mb-2">
                  <Badge variant="secondary" className={cn("text-xs", getPriorityColor(task.priority))}>
                    {task.priority}
                  </Badge>
                  <span className="text-xs text-muted-foreground">{task.project}</span>
                </div>
                
                <div className="flex items-center justify-between text-xs text-muted-foreground">
                  <span>{task.assignee}</span>
                  <span>{task.dueDate}</span>
                </div>
              </div>
              
              <DropdownMenu>
                <DropdownMenuTrigger asChild>
                  <Button variant="ghost" size="sm" className="h-6 w-6 p-0 opacity-0 group-hover:opacity-100">
                    <MoreHorizontal className="h-3 w-3" />
                  </Button>
                </DropdownMenuTrigger>
                <DropdownMenuContent align="end">
                  <DropdownMenuItem>Edit</DropdownMenuItem>
                  <DropdownMenuItem>Assign</DropdownMenuItem>
                  <DropdownMenuSeparator />
                  <DropdownMenuItem className="text-destructive">Delete</DropdownMenuItem>
                </DropdownMenuContent>
              </DropdownMenu>
            </div>
          </div>
        ))}
      </div>
    </ScrollArea>
  );
}

function ProjectsList({ searchQuery }: { searchQuery: string }) {
  const projects = [
    {
      id: '1',
      name: 'BARQ Platform',
      description: 'Enterprise AI orchestration platform',
      progress: 75,
      tasksCount: 24,
      membersCount: 8,
      status: 'active'
    },
    {
      id: '2',
      name: 'DevOps Infrastructure',
      description: 'CI/CD and deployment automation',
      progress: 45,
      tasksCount: 12,
      membersCount: 4,
      status: 'active'
    }
  ];

  const filteredProjects = projects.filter(project =>
    project.name.toLowerCase().includes(searchQuery.toLowerCase()) ||
    project.description.toLowerCase().includes(searchQuery.toLowerCase())
  );

  return (
    <ScrollArea className="h-full">
      <div className="p-4 space-y-3">
        {filteredProjects.map((project) => (
          <div
            key={project.id}
            className="p-4 rounded-lg border bg-card hover:bg-accent/50 transition-colors cursor-pointer"
          >
            <div className="flex items-start justify-between mb-2">
              <h4 className="font-medium">{project.name}</h4>
              <Badge variant="outline" className="text-xs">
                {project.status}
              </Badge>
            </div>
            
            <p className="text-sm text-muted-foreground mb-3">{project.description}</p>
            
            <div className="space-y-2">
              <div className="flex items-center justify-between text-xs">
                <span>Progress</span>
                <span>{project.progress}%</span>
              </div>
              <div className="w-full bg-secondary rounded-full h-2">
                <div 
                  className="bg-primary h-2 rounded-full transition-all"
                  style={{ width: `${project.progress}%` }}
                />
              </div>
            </div>
            
            <div className="flex items-center justify-between mt-3 text-xs text-muted-foreground">
              <span>{project.tasksCount} tasks</span>
              <span>{project.membersCount} members</span>
            </div>
          </div>
        ))}
      </div>
    </ScrollArea>
  );
}

function TeamsList({ searchQuery }: { searchQuery: string }) {
  const teams = [
    {
      id: '1',
      name: 'Development Team',
      members: ['John Doe', 'Jane Smith', 'Mike Johnson'],
      activeProjects: 2,
      role: 'Engineering'
    },
    {
      id: '2',
      name: 'DevOps Team',
      members: ['Sarah Wilson', 'Tom Brown'],
      activeProjects: 1,
      role: 'Infrastructure'
    }
  ];

  const filteredTeams = teams.filter(team =>
    team.name.toLowerCase().includes(searchQuery.toLowerCase()) ||
    team.role.toLowerCase().includes(searchQuery.toLowerCase())
  );

  return (
    <ScrollArea className="h-full">
      <div className="p-4 space-y-3">
        {filteredTeams.map((team) => (
          <div
            key={team.id}
            className="p-4 rounded-lg border bg-card hover:bg-accent/50 transition-colors cursor-pointer"
          >
            <div className="flex items-start justify-between mb-2">
              <h4 className="font-medium">{team.name}</h4>
              <Badge variant="outline" className="text-xs">
                {team.role}
              </Badge>
            </div>
            
            <div className="space-y-2">
              <div className="flex items-center gap-2">
                <Users className="h-4 w-4 text-muted-foreground" />
                <span className="text-sm">{team.members.length} members</span>
              </div>
              
              <div className="flex items-center gap-2">
                <FolderOpen className="h-4 w-4 text-muted-foreground" />
                <span className="text-sm">{team.activeProjects} active projects</span>
              </div>
            </div>
            
            <div className="mt-3">
              <div className="flex flex-wrap gap-1">
                {team.members.slice(0, 3).map((member, index) => (
                  <Badge key={index} variant="secondary" className="text-xs">
                    {member.split(' ')[0]}
                  </Badge>
                ))}
                {team.members.length > 3 && (
                  <Badge variant="secondary" className="text-xs">
                    +{team.members.length - 3}
                  </Badge>
                )}
              </div>
            </div>
          </div>
        ))}
      </div>
    </ScrollArea>
  );
}
