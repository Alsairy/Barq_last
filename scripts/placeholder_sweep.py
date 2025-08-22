#!/usr/bin/env python3
"""
Placeholder Sweep Script for BARQ Platform
Sweeps Backend + Frontend for NotImplemented/TODO/MOCK patterns
"""

import os
import re
import csv
import sys
from pathlib import Path
from typing import List, Dict

class PlaceholderSweeper:
    def __init__(self):
        self.issues = []
        
    def scan_file(self, file_path: Path, base_dir: Path) -> List[Dict]:
        """Scan a single file for placeholder patterns"""
        issues = []
        
        try:
            with open(file_path, 'r', encoding='utf-8') as f:
                content = f.read()
                lines = content.split('\n')
                
            patterns = {
                'NotImplemented': {
                    'regex': r'(NotImplementedException|throw new NotImplementedException|NotImplemented)',
                    'severity': 'High'
                },
                'TODO': {
                    'regex': r'(TODO|FIXME|HACK|XXX|BUG)',
                    'severity': 'Medium'
                },
                'Mock': {
                    'regex': r'(Mock\w+|\.Mock|MockService|FakeService|DummyService|TestService)',
                    'severity': 'High'
                },
                'Placeholder': {
                    'regex': r'(placeholder|stub|dummy|temp|temporary|sample)',
                    'severity': 'Medium'
                },
                'TaskFromResult': {
                    'regex': r'Task\.FromResult\(',
                    'severity': 'Medium'
                },
                'EmptyImplementation': {
                    'regex': r'(return\s+null;|return\s+default;|return\s+new\s+\w+\(\);)',
                    'severity': 'Medium'
                },
                'ConsoleLog': {
                    'regex': r'console\.(log|warn|error|debug)',
                    'severity': 'Low'
                },
                'DebugCode': {
                    'regex': r'(System\.Diagnostics\.Debug|Console\.WriteLine|console\.log)',
                    'severity': 'Low'
                }
            }
            
            for line_num, line in enumerate(lines, 1):
                for pattern_name, pattern_info in patterns.items():
                    if re.search(pattern_info['regex'], line, re.IGNORECASE):
                        issues.append({
                            'file': str(file_path.relative_to(base_dir)),
                            'line': line_num,
                            'severity': pattern_info['severity'],
                            'type': pattern_name,
                            'code': line.strip()
                        })
                        
        except Exception as e:
            issues.append({
                'file': str(file_path.relative_to(base_dir)),
                'line': 0,
                'severity': 'Low',
                'type': 'ScanError',
                'code': f'Failed to scan: {str(e)}'
            })
            
        return issues
    
    def scan_directory(self, directory: str, file_extensions: List[str]) -> None:
        """Scan directory for files with specified extensions"""
        base_dir = Path(directory)
        
        for ext in file_extensions:
            files = list(base_dir.rglob(f'*.{ext}'))
            
            for file_path in files:
                if any(skip in str(file_path) for skip in ['node_modules', '/bin/', '/obj/', '/.git/', '/dist/', '/build/']):
                    continue
                    
                file_issues = self.scan_file(file_path, base_dir)
                self.issues.extend(file_issues)
    
    def generate_report(self, output_file: str) -> None:
        """Generate CSV report"""
        with open(output_file, 'w', newline='', encoding='utf-8') as csvfile:
            fieldnames = ['file', 'line', 'severity', 'type', 'code']
            writer = csv.DictWriter(csvfile, fieldnames=fieldnames)
            
            writer.writeheader()
            for issue in sorted(self.issues, key=lambda x: (x['severity'], x['file'], x['line'])):
                writer.writerow(issue)
    
    def get_summary(self) -> Dict[str, int]:
        """Get summary statistics"""
        summary = {'High': 0, 'Medium': 0, 'Low': 0}
        for issue in self.issues:
            summary[issue['severity']] += 1
        return summary

def main():
    if len(sys.argv) < 3:
        print("Usage: python3 placeholder_sweep.py <backend_dir> <frontend_dir> [output_file]")
        sys.exit(1)
    
    backend_dir = sys.argv[1]
    frontend_dir = sys.argv[2]
    output_file = sys.argv[3] if len(sys.argv) > 3 else 'audit/audit_placeholders.csv'
    
    sweeper = PlaceholderSweeper()
    
    if os.path.exists(backend_dir):
        print(f"Scanning backend directory: {backend_dir}")
        sweeper.scan_directory(backend_dir, ['cs'])
    else:
        print(f"Warning: Backend directory {backend_dir} does not exist")
    
    if os.path.exists(frontend_dir):
        print(f"Scanning frontend directory: {frontend_dir}")
        sweeper.scan_directory(frontend_dir, ['ts', 'tsx', 'js', 'jsx'])
    else:
        print(f"Warning: Frontend directory {frontend_dir} does not exist")
    
    os.makedirs(os.path.dirname(output_file), exist_ok=True)
    sweeper.generate_report(output_file)
    
    summary = sweeper.get_summary()
    print(f"\nPlaceholder Sweep Complete:")
    print(f"  High: {summary['High']}")
    print(f"  Medium: {summary['Medium']}")
    print(f"  Low: {summary['Low']}")
    print(f"  Total: {len(sweeper.issues)}")
    print(f"Report saved to: {output_file}")
    
    if summary['High'] > 0:
        print(f"FAIL: Found {summary['High']} high severity placeholder issues")
        sys.exit(1)
    
    sys.exit(0)

if __name__ == '__main__':
    main()
