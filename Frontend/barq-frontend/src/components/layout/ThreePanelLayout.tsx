import React, { useState } from 'react';
import { PanelGroup, Panel, PanelResizeHandle } from 'react-resizable-panels';
import { TaskManagementPanel } from './TaskManagementPanel';
import { ChatInterface } from './ChatInterface';
import { ProgressPanel } from './ProgressPanel';
import { Header } from './Header';
import { cn } from '../../lib/utils';

export function ThreePanelLayout() {
  const [leftPanelCollapsed, setLeftPanelCollapsed] = useState(false);
  const [rightPanelCollapsed, setRightPanelCollapsed] = useState(false);

  return (
    <div className="h-screen flex flex-col">
      <Header 
        onToggleLeftPanel={() => setLeftPanelCollapsed(!leftPanelCollapsed)}
        onToggleRightPanel={() => setRightPanelCollapsed(!rightPanelCollapsed)}
        leftPanelCollapsed={leftPanelCollapsed}
        rightPanelCollapsed={rightPanelCollapsed}
      />
      
      <div className="flex-1 overflow-hidden">
        <PanelGroup direction="horizontal" className="h-full">
          {/* Left Panel - Task Management */}
          <Panel
            defaultSize={25}
            minSize={15}
            maxSize={40}
            collapsible={true}
            className={cn(
              "transition-all duration-300",
              leftPanelCollapsed && "min-w-0"
            )}
          >
            <TaskManagementPanel collapsed={leftPanelCollapsed} />
          </Panel>

          <PanelResizeHandle className="w-2 bg-border hover:bg-primary/20 transition-colors" />

          {/* Center Panel - Chat Interface */}
          <Panel defaultSize={50} minSize={30}>
            <ChatInterface />
          </Panel>

          <PanelResizeHandle className="w-2 bg-border hover:bg-primary/20 transition-colors" />

          {/* Right Panel - Progress & Documents */}
          <Panel
            defaultSize={25}
            minSize={15}
            maxSize={40}
            collapsible={true}
            className={cn(
              "transition-all duration-300",
              rightPanelCollapsed && "min-w-0"
            )}
          >
            <ProgressPanel collapsed={rightPanelCollapsed} />
          </Panel>
        </PanelGroup>
      </div>
    </div>
  );
}
