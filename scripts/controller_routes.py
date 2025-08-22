#!/usr/bin/env python3
"""
API Route Extractor for BARQ Platform
Lists all controller routes from ASP.NET Core controllers
"""

import os
import re
import json
import sys
from pathlib import Path
from typing import List, Dict

class RouteExtractor:
    def __init__(self, controllers_dir: str):
        self.controllers_dir = Path(controllers_dir)
        self.routes = []
        
    def extract_routes_from_file(self, file_path: Path) -> List[Dict]:
        """Extract routes from a single controller file"""
        routes = []
        
        try:
            with open(file_path, 'r', encoding='utf-8') as f:
                content = f.read()
                
            controller_match = re.search(r'class\s+(\w+)Controller', content)
            if not controller_match:
                return routes
                
            controller_name = controller_match.group(1)
            
            route_prefix = ""
            route_attr_match = re.search(r'\[Route\("([^"]+)"\)\]', content)
            if route_attr_match:
                route_prefix = route_attr_match.group(1)
            else:
                route_prefix = f"api/{controller_name.lower()}"
            
            action_pattern = r'\[(HttpGet|HttpPost|HttpPut|HttpDelete|HttpPatch)(?:\("([^"]+)"\))?\]\s*(?:\[.*?\]\s*)*public\s+(?:async\s+)?(?:Task<?[^>]*>?\s+)?(\w+)\s*\('
            
            for match in re.finditer(action_pattern, content, re.MULTILINE | re.DOTALL):
                http_method = match.group(1).replace('Http', '').upper()
                route_template = match.group(2) or ""
                action_name = match.group(3)
                
                if route_template:
                    if route_template.startswith('/'):
                        full_route = route_template
                    else:
                        full_route = f"/{route_prefix}/{route_template}"
                else:
                    full_route = f"/{route_prefix}/{action_name.lower()}"
                
                full_route = full_route.replace('//', '/').replace('[controller]', controller_name.lower())
                
                routes.append({
                    'controller': controller_name,
                    'action': action_name,
                    'method': http_method,
                    'route': full_route,
                    'file': str(file_path.relative_to(self.controllers_dir.parent))
                })
                
        except Exception as e:
            print(f"Error processing {file_path}: {e}", file=sys.stderr)
            
        return routes
    
    def extract_all_routes(self) -> None:
        """Extract routes from all controller files"""
        controller_files = list(self.controllers_dir.rglob('*Controller.cs'))
        
        for file_path in controller_files:
            file_routes = self.extract_routes_from_file(file_path)
            self.routes.extend(file_routes)
    
    def get_routes_json(self) -> str:
        """Get routes as JSON string"""
        return json.dumps(self.routes, indent=2)

def main():
    if len(sys.argv) != 2:
        print("Usage: python3 controller_routes.py <controllers_directory>")
        sys.exit(1)
    
    controllers_dir = sys.argv[1]
    
    if not os.path.exists(controllers_dir):
        print(f"Error: Controllers directory {controllers_dir} does not exist")
        sys.exit(1)
    
    extractor = RouteExtractor(controllers_dir)
    extractor.extract_all_routes()
    
    print(extractor.get_routes_json())

if __name__ == '__main__':
    main()
