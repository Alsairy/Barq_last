import React, { useState, useRef, useEffect } from 'react';
import { Button } from '../ui/button';
import { Input } from '../ui/input';
import { ScrollArea } from '../ui/scroll-area';
import { Badge } from '../ui/badge';
import { Avatar, AvatarFallback, AvatarImage } from '../ui/avatar';
import { 
  Send, 
  Paperclip, 
  Mic, 
  MoreHorizontal, 
  Bot, 
  User, 
  Loader2,
  RefreshCw,
  Settings
} from 'lucide-react';
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from '../ui/dropdown-menu';
import { cn } from '../../lib/utils';
import { useOptimisticUpdate } from '../../hooks/useOptimisticUpdate';
import { useDraftSave } from '../../hooks/useDraftSave';
import { useI18n } from '../../hooks/useI18n';
import { toast } from 'sonner';

interface Message {
  id: string;
  content: string;
  sender: 'user' | 'ai';
  timestamp: Date;
  status: 'sending' | 'sent' | 'error';
  attachments?: Array<{
    id: string;
    name: string;
    type: string;
    size: number;
  }>;
}

interface AIAgent {
  id: string;
  name: string;
  avatar?: string;
  status: 'online' | 'busy' | 'offline';
  capabilities: string[];
}

export function ChatInterface() {
  const [messages, setMessages] = useState<Message[]>([
    {
      id: '1',
      content: 'Hello! I\'m your AI assistant. How can I help you with your workflow today?',
      sender: 'ai',
      timestamp: new Date(Date.now() - 60000),
      status: 'sent'
    }
  ]);
  const [isTyping, setIsTyping] = useState(false);
  const [selectedAgent, setSelectedAgent] = useState<AIAgent>({
    id: '1',
    name: 'BARQ Assistant',
    status: 'online',
    capabilities: ['Task Management', 'Workflow Automation', 'Document Analysis']
  });

  const { t, isRTL } = useI18n();
  
  const { data: inputValue, updateData: setInputValue, isDirty } = useDraftSave('', {
    key: 'chat-input',
    saveInterval: 10000
  });

  const { execute: sendMessage, isLoading: isSending } = useOptimisticUpdate(
    async (message: string) => {
      await new Promise(resolve => setTimeout(resolve, 1000));
      return { success: true };
    },
    {
      successMessage: t('message_sent', 'Message sent'),
      errorMessage: t('message_failed', 'Failed to send message')
    }
  );

  const messagesEndRef = useRef<HTMLDivElement>(null);
  const inputRef = useRef<HTMLInputElement>(null);

  const scrollToBottom = () => {
    messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' });
  };

  useEffect(() => {
    scrollToBottom();
  }, [messages]);

  const handleSendMessage = async () => {
    if (!inputValue.trim()) return;

    const newMessage: Message = {
      id: Date.now().toString(),
      content: inputValue,
      sender: 'user',
      timestamp: new Date(),
      status: 'sending'
    };

    setMessages(prev => [...prev, newMessage]);
    setInputValue('');
    setIsTyping(true);

    setTimeout(() => {
      setMessages(prev => 
        prev.map(msg => 
          msg.id === newMessage.id 
            ? { ...msg, status: 'sent' as const }
            : msg
        )
      );

      setTimeout(() => {
        const aiResponse: Message = {
          id: (Date.now() + 1).toString(),
          content: generateAIResponse(inputValue),
          sender: 'ai',
          timestamp: new Date(),
          status: 'sent'
        };
        setMessages(prev => [...prev, aiResponse]);
        setIsTyping(false);
      }, 1000);
    }, 500);
  };

  const generateAIResponse = (userMessage: string): string => {
    const responses = [
      "I understand you need help with that. Let me analyze the current workflow and provide recommendations.",
      "Based on your request, I can help you create a new task or update an existing workflow. Would you like me to proceed?",
      "I've reviewed the available options. Here are the steps I recommend for your workflow optimization.",
      "That's a great question! Let me break down the process and show you the best approach.",
      "I can assist you with that task. Would you like me to create a workflow template or help with the current process?"
    ];
    return responses[Math.floor(Math.random() * responses.length)];
  };

  const handleKeyPress = (e: React.KeyboardEvent) => {
    if (e.key === 'Enter' && !e.shiftKey) {
      e.preventDefault();
      handleSendMessage();
    }
  };

  return (
    <div className="h-full flex flex-col bg-background">
      {/* Header */}
      <div className="p-4 border-b bg-muted/30">
        <div className="flex items-center justify-between">
          <div className="flex items-center gap-3">
            <Avatar className="h-8 w-8">
              <AvatarImage src={selectedAgent.avatar} />
              <AvatarFallback>
                <Bot className="h-4 w-4" />
              </AvatarFallback>
            </Avatar>
            <div>
              <h3 className="font-medium text-sm">{selectedAgent.name}</h3>
              <div className="flex items-center gap-2">
                <div className={cn(
                  "w-2 h-2 rounded-full",
                  selectedAgent.status === 'online' && "bg-green-500",
                  selectedAgent.status === 'busy' && "bg-yellow-500",
                  selectedAgent.status === 'offline' && "bg-gray-500"
                )} />
                <span className="text-xs text-muted-foreground capitalize">
                  {selectedAgent.status}
                </span>
              </div>
            </div>
          </div>

          <div className="flex items-center gap-1">
            <Button variant="ghost" size="sm" className="h-8 w-8 p-0">
              <RefreshCw className="h-4 w-4" />
            </Button>
            <DropdownMenu>
              <DropdownMenuTrigger asChild>
                <Button variant="ghost" size="sm" className="h-8 w-8 p-0">
                  <MoreHorizontal className="h-4 w-4" />
                </Button>
              </DropdownMenuTrigger>
              <DropdownMenuContent align="end">
                <DropdownMenuItem>
                  <Settings className="mr-2 h-4 w-4" />
                  Agent Settings
                </DropdownMenuItem>
                <DropdownMenuItem>Clear Chat</DropdownMenuItem>
                <DropdownMenuSeparator />
                <DropdownMenuItem>Export Conversation</DropdownMenuItem>
              </DropdownMenuContent>
            </DropdownMenu>
          </div>
        </div>

        {/* Agent Capabilities */}
        <div className="flex flex-wrap gap-1 mt-2">
          {selectedAgent.capabilities.map((capability, index) => (
            <Badge key={index} variant="secondary" className="text-xs">
              {capability}
            </Badge>
          ))}
        </div>
      </div>

      {/* Messages */}
      <ScrollArea className="flex-1 p-4">
        <div className="space-y-4">
          {messages.map((message) => (
            <div
              key={message.id}
              className={cn(
                "flex gap-3",
                message.sender === 'user' && "flex-row-reverse"
              )}
            >
              <Avatar className="h-8 w-8 flex-shrink-0">
                <AvatarFallback>
                  {message.sender === 'user' ? (
                    <User className="h-4 w-4" />
                  ) : (
                    <Bot className="h-4 w-4" />
                  )}
                </AvatarFallback>
              </Avatar>

              <div className={cn(
                "flex flex-col gap-1 max-w-[80%]",
                message.sender === 'user' && "items-end"
              )}>
                <div className={cn(
                  "rounded-lg px-3 py-2 text-sm",
                  message.sender === 'user' 
                    ? "bg-primary text-primary-foreground" 
                    : "bg-muted"
                )}>
                  {message.content}
                </div>

                <div className="flex items-center gap-2 text-xs text-muted-foreground">
                  <span>{message.timestamp.toLocaleTimeString()}</span>
                  {message.sender === 'user' && (
                    <span className={cn(
                      message.status === 'sending' && "text-yellow-500",
                      message.status === 'sent' && "text-green-500",
                      message.status === 'error' && "text-red-500"
                    )}>
                      {message.status === 'sending' && <Loader2 className="h-3 w-3 animate-spin" />}
                      {message.status === 'sent' && '✓'}
                      {message.status === 'error' && '✗'}
                    </span>
                  )}
                </div>
              </div>
            </div>
          ))}

          {isTyping && (
            <div className="flex gap-3">
              <Avatar className="h-8 w-8">
                <AvatarFallback>
                  <Bot className="h-4 w-4" />
                </AvatarFallback>
              </Avatar>
              <div className="bg-muted rounded-lg px-3 py-2">
                <div className="flex gap-1">
                  <div className="w-2 h-2 bg-muted-foreground rounded-full animate-bounce" />
                  <div className="w-2 h-2 bg-muted-foreground rounded-full animate-bounce" style={{ animationDelay: '0.1s' }} />
                  <div className="w-2 h-2 bg-muted-foreground rounded-full animate-bounce" style={{ animationDelay: '0.2s' }} />
                </div>
              </div>
            </div>
          )}

          <div ref={messagesEndRef} />
        </div>
      </ScrollArea>

      {/* Input */}
      <div className="p-4 border-t bg-muted/30">
        <div className="flex items-end gap-2">
          <div className="flex-1 relative">
            <Input
              ref={inputRef}
              value={inputValue}
              onChange={(e) => setInputValue(e.target.value)}
              onKeyPress={handleKeyPress}
              placeholder="Type your message..."
              className="pr-20"
              disabled={isTyping}
            />
            <div className="absolute right-2 top-1/2 -translate-y-1/2 flex items-center gap-1">
              <Button variant="ghost" size="sm" className="h-6 w-6 p-0">
                <Paperclip className="h-3 w-3" />
              </Button>
              <Button variant="ghost" size="sm" className="h-6 w-6 p-0">
                <Mic className="h-3 w-3" />
              </Button>
            </div>
          </div>
          <Button 
            onClick={handleSendMessage}
            disabled={!inputValue.trim() || isTyping}
            size="sm"
          >
            <Send className="h-4 w-4" />
          </Button>
        </div>
      </div>
    </div>
  );
}
