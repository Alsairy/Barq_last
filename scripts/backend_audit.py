#!/usr/bin/env python3
"""
Backend Static Code Audit Script for BARQ Platform
Scans for NotImplemented, TODO, mocks, HttpClient, FromSqlRaw, tenant gaps
"""

import os
import re
import csv
import argparse
import sys
from pathlib import Path
from typing import List, Dict, Tuple

class BackendAuditor:
    def __init__(self, src_dir: str):
        self.src_dir = Path(src_dir)
        self.issues = []
        
    def scan_file(self, file_path: Path) -> List[Dict]:
        """Scan a single file for issues"""
        issues = []
        
        try:
            with open(file_path, 'r', encoding='utf-8') as f:
                content = f.read()
                lines = content.split('\n')
                
            patterns = {
                'NotImplemented': {
                    'regex': r'(NotImplementedException|throw new NotImplementedException|NotImplemented)',
                    'severity': 'High',
                    'description': 'NotImplemented exception or placeholder'
                },
                'TODO': {
                    'regex': r'(TODO|FIXME|HACK|XXX)',
                    'severity': 'Medium',
                    'description': 'TODO/FIXME comment'
                },
                'Mock': {
                    'regex': r'(Mock\w+|\.Mock|MockService|FakeService)',
                    'severity': 'High',
                    'description': 'Mock or fake service in production code'
                },
                'HttpClient': {
                    'regex': r'new HttpClient\(\)',
                    'severity': 'Medium',
                    'description': 'Direct HttpClient instantiation (should use IHttpClientFactory)'
                },
                'FromSqlRaw': {
                    'regex': r'FromSqlRaw\(',
                    'severity': 'Medium',
                    'description': 'Raw SQL query (potential SQL injection risk)'
                },
                'TenantGap': {
                    'regex': r'\.Where\([^)]*(?<!TenantId)\s*==\s*[^)]*\)',
                    'severity': 'High',
                    'description': 'Query without tenant filtering'
                },
                'TaskFromResult': {
                    'regex': r'Task\.FromResult\(',
                    'severity': 'Medium',
                    'description': 'Task.FromResult placeholder implementation'
                },
                'EmptyMethod': {
                    'regex': r'{\s*return\s*[^;]*;\s*}',
                    'severity': 'Medium',
                    'description': 'Potentially empty method implementation'
                },
                'Placeholder': {
                    'regex': r'(placeholder|stub|dummy|temp)',
                    'severity': 'Medium',
                    'description': 'Placeholder text in code'
                }
            }
            
            for line_num, line in enumerate(lines, 1):
                for pattern_name, pattern_info in patterns.items():
                    if re.search(pattern_info['regex'], line, re.IGNORECASE):
                        issues.append({
                            'file': str(file_path.relative_to(self.src_dir)),
                            'line': line_num,
                            'severity': pattern_info['severity'],
                            'type': pattern_name,
                            'description': pattern_info['description'],
                            'code': line.strip()
                        })
                        
        except Exception as e:
            issues.append({
                'file': str(file_path.relative_to(self.src_dir)),
                'line': 0,
                'severity': 'Low',
                'type': 'ScanError',
                'description': f'Failed to scan file: {str(e)}',
                'code': ''
            })
            
        return issues
    
    def scan_directory(self) -> None:
        """Scan all C# files in the directory"""
        cs_files = list(self.src_dir.rglob('*.cs'))
        
        for file_path in cs_files:
            if '/test' in str(file_path).lower() or 'test' in file_path.name.lower():
                continue
                
            file_issues = self.scan_file(file_path)
            self.issues.extend(file_issues)
    
    def generate_report(self, output_file: str) -> None:
        """Generate CSV report"""
        with open(output_file, 'w', newline='', encoding='utf-8') as csvfile:
            fieldnames = ['file', 'line', 'severity', 'type', 'description', 'code']
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
    parser = argparse.ArgumentParser(description='Backend Static Code Audit')
    parser.add_argument('--src', required=True, help='Source directory to scan')
    parser.add_argument('--out', required=True, help='Output CSV file')
    parser.add_argument('--fail-on', choices=['High', 'Medium', 'Low'], 
                       help='Fail if issues of this severity or higher are found')
    
    args = parser.parse_args()
    
    if not os.path.exists(args.src):
        print(f"Error: Source directory {args.src} does not exist")
        sys.exit(1)
    
    auditor = BackendAuditor(args.src)
    auditor.scan_directory()
    auditor.generate_report(args.out)
    
    summary = auditor.get_summary()
    print(f"Backend Audit Complete:")
    print(f"  High: {summary['High']}")
    print(f"  Medium: {summary['Medium']}")
    print(f"  Low: {summary['Low']}")
    print(f"  Total: {len(auditor.issues)}")
    print(f"Report saved to: {args.out}")
    
    if args.fail_on:
        severity_levels = {'High': 3, 'Medium': 2, 'Low': 1}
        fail_level = severity_levels[args.fail_on]
        
        for issue in auditor.issues:
            if severity_levels[issue['severity']] >= fail_level:
                print(f"FAIL: Found {issue['severity']} severity issue")
                sys.exit(1)
    
    sys.exit(0)

if __name__ == '__main__':
    main()
