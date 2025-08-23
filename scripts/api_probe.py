#!/usr/bin/env python3
"""
API Probe Script for BARQ Platform
Hits API routes and records status/latency
"""

import json
import csv
import time
import requests
import sys
import os
import argparse
from typing import List, Dict
from urllib.parse import urljoin

class ApiProber:
    def __init__(self, base_url: str, timeout: int = 10):
        self.base_url = base_url.rstrip('/')
        self.timeout = timeout
        self.session = requests.Session()
        self.results = []
        
    def probe_route(self, route_info: Dict) -> Dict:
        """Probe a single API route"""
        url = urljoin(self.base_url, route_info['route'].lstrip('/'))
        method = route_info['method'].upper()
        
        start_time = time.time()
        
        try:
            response = self.session.request(
                method=method,
                url=url,
                timeout=self.timeout,
                allow_redirects=False
            )
            
            latency = (time.time() - start_time) * 1000  # Convert to ms
            
            if response.status_code in (401, 403):
                note = "Auth required (treated as pass)"
                error = ''
            else:
                note = ''
                error = ''
            
            result = {
                'controller': route_info['controller'],
                'action': route_info['action'],
                'method': method,
                'route': route_info['route'],
                'url': url,
                'status_code': response.status_code,
                'latency_ms': round(latency, 2),
                'content_length': len(response.content),
                'error': error,
                'note': note
            }
            
        except requests.exceptions.Timeout:
            result = {
                'controller': route_info['controller'],
                'action': route_info['action'],
                'method': method,
                'route': route_info['route'],
                'url': url,
                'status_code': 0,
                'latency_ms': self.timeout * 1000,
                'content_length': 0,
                'error': 'Timeout'
            }
            
        except requests.exceptions.ConnectionError:
            result = {
                'controller': route_info['controller'],
                'action': route_info['action'],
                'method': method,
                'route': route_info['route'],
                'url': url,
                'status_code': 0,
                'latency_ms': 0,
                'content_length': 0,
                'error': 'Connection Error'
            }
            
        except Exception as e:
            result = {
                'controller': route_info['controller'],
                'action': route_info['action'],
                'method': method,
                'route': route_info['route'],
                'url': url,
                'status_code': 0,
                'latency_ms': 0,
                'content_length': 0,
                'error': str(e)
            }
        
        return result
    
    def probe_all_routes(self, routes: List[Dict]) -> None:
        """Probe all routes"""
        print(f"Probing {len(routes)} routes...")
        
        for i, route_info in enumerate(routes):
            result = self.probe_route(route_info)
            self.results.append(result)
            
            print(f"[{i+1}/{len(routes)}] {result['method']} {result['route']} -> {result['status_code']} ({result['latency_ms']}ms)")
    
    def generate_report(self, output_file: str) -> None:
        """Generate CSV report"""
        with open(output_file, 'w', newline='', encoding='utf-8') as csvfile:
            fieldnames = ['controller', 'action', 'method', 'route', 'url', 'status_code', 'latency_ms', 'content_length', 'error', 'note']
            writer = csv.DictWriter(csvfile, fieldnames=fieldnames)
            
            writer.writeheader()
            for result in self.results:
                writer.writerow(result)
    
    def get_summary(self) -> Dict:
        """Get summary statistics"""
        total = len(self.results)
        success = len([r for r in self.results if 200 <= r['status_code'] < 300])
        client_errors = len([r for r in self.results if 400 <= r['status_code'] < 500])
        server_errors = len([r for r in self.results if 500 <= r['status_code'] < 600])
        connection_errors = len([r for r in self.results if r['status_code'] == 0])
        
        avg_latency = sum(r['latency_ms'] for r in self.results if r['latency_ms'] > 0) / max(1, total - connection_errors)
        
        return {
            'total': total,
            'success': success,
            'client_errors': client_errors,
            'server_errors': server_errors,
            'connection_errors': connection_errors,
            'avg_latency_ms': round(avg_latency, 2)
        }

def main():
    parser = argparse.ArgumentParser(description='API Route Prober')
    parser.add_argument('base_url', nargs='?', help='Base URL of the API')
    parser.add_argument('routes_file', help='JSON file containing routes')
    parser.add_argument('output_file', help='Output CSV file')
    parser.add_argument('--timeout', type=int, default=10, help='Request timeout in seconds')
    
    args = parser.parse_args()
    
    import os
    BASE = args.base_url if args.base_url else os.getenv("API_BASE_URL", "http://127.0.0.1:5080")
    
    try:
        with open(args.routes_file, 'r') as f:
            routes = json.load(f)
    except Exception as e:
        print(f"Error loading routes file: {e}")
        sys.exit(1)
    
    prober = ApiProber(BASE, args.timeout)
    prober.probe_all_routes(routes)
    prober.generate_report(args.output_file)
    
    summary = prober.get_summary()
    print(f"\nAPI Probe Summary:")
    print(f"  Total routes: {summary['total']}")
    print(f"  Success (2xx): {summary['success']}")
    print(f"  Client errors (4xx): {summary['client_errors']}")
    print(f"  Server errors (5xx): {summary['server_errors']}")
    print(f"  Connection errors: {summary['connection_errors']}")
    print(f"  Average latency: {summary['avg_latency_ms']}ms")
    print(f"Report saved to: {args.output_file}")

if __name__ == '__main__':
    main()
