import csv

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
            if len(issues) <= 5:
                for issue in issues:
                    print(f'  {issue["file"]}:{issue["line"]} - {issue["description"]}')

def analyze_frontend_audit():
    print("\n=== FRONTEND AUDIT ANALYSIS ===")
    with open('audit/audit_frontend.csv', 'r') as f:
        reader = csv.DictReader(f)
        high_issues = [row for row in reader if row['severity'] == 'High']
        print(f'High severity frontend issues: {len(high_issues)}')
        
        for i, issue in enumerate(high_issues[:10]):
            print(f'{i+1}. {issue["file"]}:{issue["line"]} - {issue["type"]} - {issue["description"]}')

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
            for issue in issues[:5]:  # Show first 5 of each type
                print(f'  {issue["file"]}:{issue["line"]}')

if __name__ == "__main__":
    analyze_backend_audit()
    analyze_frontend_audit()
    analyze_placeholder_audit()
