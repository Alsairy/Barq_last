import csv
import sys

csv.field_size_limit(sys.maxsize)

def analyze_backend_audit():
    print("=== BACKEND AUDIT ANALYSIS ===")
    with open('audit/audit_backend.csv', 'r') as f:
        reader = csv.DictReader(f)
        high_issues = [row for row in reader if row['severity'] == 'High']
        print(f'High severity backend issues: {len(high_issues)}')
        
        types = {}
        for issue in high_issues:
            issue_type = issue['type']
            if issue_type not in types:
                types[issue_type] = []
            types[issue_type].append(issue)
        
        for issue_type, issues in types.items():
            print(f'{issue_type}: {len(issues)} issues')
            files = set()
            for issue in issues:
                files.add(issue['file'])
                if len(files) >= 10:
                    break
            for file in sorted(files):
                print(f'  {file}')

def analyze_frontend_audit():
    print("\n=== FRONTEND AUDIT ANALYSIS ===")
    try:
        with open('audit/audit_frontend.csv', 'r') as f:
            reader = csv.DictReader(f)
            high_issues = [row for row in reader if row['severity'] == 'High']
            print(f'High severity frontend issues: {len(high_issues)}')
            
            types = {}
            for issue in high_issues:
                issue_type = issue['type']
                if issue_type not in types:
                    types[issue_type] = []
                types[issue_type].append(issue)
            
            for issue_type, issues in types.items():
                print(f'{issue_type}: {len(issues)} issues')
                files = set()
                for issue in issues:
                    files.add(issue['file'])
                    if len(files) >= 5:
                        break
                for file in sorted(files):
                    print(f'  {file}')
    except Exception as e:
        print(f"Error analyzing frontend audit: {e}")

def analyze_placeholder_audit():
    print("\n=== PLACEHOLDER AUDIT ANALYSIS ===")
    with open('audit/audit_placeholders.csv', 'r') as f:
        reader = csv.DictReader(f)
        high_issues = [row for row in reader if row['severity'] == 'High']
        print(f'High severity placeholder issues: {len(high_issues)}')
        
        types = {}
        for issue in high_issues:
            issue_type = issue['type']
            if issue_type not in types:
                types[issue_type] = []
            types[issue_type].append(issue)
        
        for issue_type, issues in types.items():
            print(f'{issue_type}: {len(issues)} issues')
            files = set()
            for issue in issues:
                files.add(issue['file'])
                if len(files) >= 5:
                    break
            for file in sorted(files):
                print(f'  {file}')

if __name__ == "__main__":
    analyze_backend_audit()
    analyze_frontend_audit()
    analyze_placeholder_audit()
