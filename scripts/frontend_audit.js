#!/usr/bin/env node
/**
 * Frontend Static Code Audit Script for BARQ Platform
 * Finds dead buttons, inert anchors, no-op handlers, TODO/MOCK
 */

const fs = require('fs');
const path = require('path');
let minimist;
try {
    minimist = require('minimist');
} catch (e) {
    console.error('Error: minimist package not found. Please install it with: npm install minimist');
    process.exit(1);
}

class FrontendAuditor {
    constructor(srcDir) {
        this.srcDir = srcDir;
        this.issues = [];
    }

    scanFile(filePath) {
        const issues = [];
        
        try {
            const content = fs.readFileSync(filePath, 'utf8');
            const lines = content.split('\n');
            const relativePath = path.relative(this.srcDir, filePath);
            
            const patterns = {
                'DeadButton': {
                    regex: /<button[^>]*(?!onClick)[^>]*>/gi,
                    severity: 'High',
                    description: 'Button without onClick handler'
                },
                'InertAnchor': {
                    regex: /<a[^>]*(?!href)[^>]*>/gi,
                    severity: 'High', 
                    description: 'Anchor without href'
                },
                'NoOpHandler': {
                    regex: /(onClick=\{\(\)\s*=>\s*\{\s*\}\}|onClick=\{undefined\}|onClick=\{null\})/gi,
                    severity: 'High',
                    description: 'No-op click handler'
                },
                'TODO': {
                    regex: /(TODO|FIXME|HACK|XXX)/gi,
                    severity: 'Medium',
                    description: 'TODO/FIXME comment'
                },
                'Mock': {
                    regex: /(MockData|FakeData|DummyData|placeholder)/gi,
                    severity: 'High',
                    description: 'Mock or placeholder data'
                },
                'Console': {
                    regex: /console\.(log|warn|error|debug)/gi,
                    severity: 'Low',
                    description: 'Console statement (should be removed in production)'
                },
                'EmptyFunction': {
                    regex: /=\s*\(\)\s*=>\s*\{\s*\}/gi,
                    severity: 'Medium',
                    description: 'Empty arrow function'
                },
                'UnusedImport': {
                    regex: /import\s+.*\s+from\s+['"][^'"]*['"];\s*$/gm,
                    severity: 'Low',
                    description: 'Potentially unused import'
                }
            };

            lines.forEach((line, index) => {
                Object.entries(patterns).forEach(([patternName, patternInfo]) => {
                    const matches = line.match(patternInfo.regex);
                    if (matches) {
                        matches.forEach(match => {
                            issues.push({
                                file: relativePath,
                                line: index + 1,
                                severity: patternInfo.severity,
                                type: patternName,
                                description: patternInfo.description,
                                code: line.trim()
                            });
                        });
                    }
                });
            });

        } catch (error) {
            issues.push({
                file: path.relative(this.srcDir, filePath),
                line: 0,
                severity: 'Low',
                type: 'ScanError',
                description: `Failed to scan file: ${error.message}`,
                code: ''
            });
        }

        return issues;
    }

    scanDirectory() {
        const scanDir = (dir) => {
            const entries = fs.readdirSync(dir, { withFileTypes: true });
            
            entries.forEach(entry => {
                const fullPath = path.join(dir, entry.name);
                
                if (entry.isDirectory() && !entry.name.startsWith('.') && entry.name !== 'node_modules') {
                    scanDir(fullPath);
                } else if (entry.isFile() && (entry.name.endsWith('.tsx') || entry.name.endsWith('.ts') || entry.name.endsWith('.jsx') || entry.name.endsWith('.js'))) {
                    const fileIssues = this.scanFile(fullPath);
                    this.issues.push(...fileIssues);
                }
            });
        };

        scanDir(this.srcDir);
    }

    generateReport(outputFile) {
        const csvContent = [
            'file,line,severity,type,description,code',
            ...this.issues
                .sort((a, b) => {
                    const severityOrder = { 'High': 3, 'Medium': 2, 'Low': 1 };
                    return severityOrder[b.severity] - severityOrder[a.severity] || 
                           a.file.localeCompare(b.file) || 
                           a.line - b.line;
                })
                .map(issue => [
                    issue.file,
                    issue.line,
                    issue.severity,
                    issue.type,
                    `"${issue.description.replace(/"/g, '""')}"`,
                    `"${issue.code.replace(/"/g, '""')}"`
                ].join(','))
        ].join('\n');

        fs.writeFileSync(outputFile, csvContent, 'utf8');
    }

    getSummary() {
        const summary = { High: 0, Medium: 0, Low: 0 };
        this.issues.forEach(issue => {
            summary[issue.severity]++;
        });
        return summary;
    }
}

function main() {
    const args = minimist(process.argv.slice(2));
    
    if (!args.src || !args.out) {
        console.error('Usage: node frontend_audit.js --src <source_dir> --out <output_file> [--fail-on High|Medium|Low]');
        process.exit(1);
    }

    if (!fs.existsSync(args.src)) {
        console.error(`Error: Source directory ${args.src} does not exist`);
        process.exit(1);
    }

    const auditor = new FrontendAuditor(args.src);
    auditor.scanDirectory();
    auditor.generateReport(args.out);

    const summary = auditor.getSummary();
    console.log('Frontend Audit Complete:');
    console.log(`  High: ${summary.High}`);
    console.log(`  Medium: ${summary.Medium}`);
    console.log(`  Low: ${summary.Low}`);
    console.log(`  Total: ${auditor.issues.length}`);
    console.log(`Report saved to: ${args.out}`);

    if (args['fail-on']) {
        const severityLevels = { 'High': 3, 'Medium': 2, 'Low': 1 };
        const failLevel = severityLevels[args['fail-on']];
        
        for (const issue of auditor.issues) {
            if (severityLevels[issue.severity] >= failLevel) {
                console.log(`FAIL: Found ${issue.severity} severity issue`);
                process.exit(1);
            }
        }
    }

    process.exit(0);
}

if (require.main === module) {
    main();
}
