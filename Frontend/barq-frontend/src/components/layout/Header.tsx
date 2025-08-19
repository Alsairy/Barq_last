import React from 'react';
import { Button } from '../ui/button';
import { PanelLeftClose, PanelLeftOpen, PanelRightClose, PanelRightOpen, Settings, Bell, User } from 'lucide-react';
import { ThemeToggle } from '../ui/theme-toggle';
import TenantSwitcher from '../../features/tenants/TenantSwitcher';
import { NotificationBell } from '../../features/notifications/NotificationBell';
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from '../ui/dropdown-menu';
import { Avatar, AvatarFallback, AvatarImage } from '../ui/avatar';

interface HeaderProps {
  onToggleLeftPanel: () => void;
  onToggleRightPanel: () => void;
  leftPanelCollapsed: boolean;
  rightPanelCollapsed: boolean;
}

export function Header({ 
  onToggleLeftPanel, 
  onToggleRightPanel, 
  leftPanelCollapsed, 
  rightPanelCollapsed 
}: HeaderProps) {
  return (
    <header className="h-14 border-b bg-background/95 backdrop-blur supports-[backdrop-filter]:bg-background/60">
      <div className="flex h-full items-center justify-between px-4">
        {/* Left Section */}
        <div className="flex items-center gap-2">
          <Button
            variant="ghost"
            size="sm"
            onClick={onToggleLeftPanel}
            className="h-8 w-8 p-0"
          >
            {leftPanelCollapsed ? (
              <PanelLeftOpen className="h-4 w-4" />
            ) : (
              <PanelLeftClose className="h-4 w-4" />
            )}
          </Button>
          
          <div className="flex items-center gap-2">
            <div className="h-6 w-6 rounded bg-primary flex items-center justify-center">
              <span className="text-primary-foreground text-xs font-bold">B</span>
            </div>
            <span className="font-semibold text-lg">BARQ</span>
          </div>
        </div>

        {/* Center Section */}
        <div className="flex items-center gap-4">
          <TenantSwitcher />
        </div>

        {/* Right Section */}
        <div className="flex items-center gap-2">
          <NotificationBell />
          <ThemeToggle />
          
          <DropdownMenu>
            <DropdownMenuTrigger asChild>
              <Button variant="ghost" className="relative h-8 w-8 rounded-full">
                <Avatar className="h-8 w-8">
                  <AvatarImage src="/avatars/01.png" alt="User" />
                  <AvatarFallback>
                    <User className="h-4 w-4" />
                  </AvatarFallback>
                </Avatar>
              </Button>
            </DropdownMenuTrigger>
            <DropdownMenuContent className="w-56" align="end" forceMount>
              <DropdownMenuItem>
                <User className="mr-2 h-4 w-4" />
                <span>Profile</span>
              </DropdownMenuItem>
              <DropdownMenuItem>
                <Settings className="mr-2 h-4 w-4" />
                <span>Settings</span>
              </DropdownMenuItem>
              <DropdownMenuSeparator />
              <DropdownMenuItem>
                <span>Log out</span>
              </DropdownMenuItem>
            </DropdownMenuContent>
          </DropdownMenu>

          <Button
            variant="ghost"
            size="sm"
            onClick={onToggleRightPanel}
            className="h-8 w-8 p-0"
          >
            {rightPanelCollapsed ? (
              <PanelRightOpen className="h-4 w-4" />
            ) : (
              <PanelRightClose className="h-4 w-4" />
            )}
          </Button>
        </div>
      </div>
    </header>
  );
}
