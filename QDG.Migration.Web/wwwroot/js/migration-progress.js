class MigrationProgressTracker {
    constructor(sessionId) {
        this.sessionId = sessionId;
        this.pollInterval = null;
        this.startTime = null;
        this.lastRowCount = 0;
        this.lastUpdateTime = Date.now();
    }

    start() {
        this.startTime = Date.now();
        this.pollInterval = setInterval(() => this.fetchProgress(), 1000);
        this.fetchProgress();
    }

    stop() {
        if (this.pollInterval) {
            clearInterval(this.pollInterval);
            this.pollInterval = null;
        }
    }

    async fetchProgress() {
        try {
            const response = await fetch(`/api/migration/progress/${this.sessionId}`);
            const data = await response.json();
            
            this.updateUI(data);

            if (data.isComplete) {
                this.stop();
                this.showCompletionButtons();
            }
        } catch (error) {
            console.error('Error fetching progress:', error);
        }
    }

    updateUI(data) {
        const totalTables = data.totalTables || 1;
        const completedTables = data.completedTables || 0;
        const progressPercent = Math.round((completedTables / totalTables) * 100);

        $('#overallProgress')
            .css('width', `${progressPercent}%`)
            .text(`${progressPercent}%`);

        $('#completedTables').text(completedTables);
        $('#totalTables').text(totalTables);
        $('#rowsCopied').text(data.totalRowsCopied.toLocaleString());
        $('#errorCount').text(data.errorCount);
        $('#status').text(data.status);

        if (typeof data.elapsed === 'number') {
            const seconds = Math.floor(data.elapsed);
            const hours = Math.floor(seconds / 3600);
            const minutes = Math.floor((seconds % 3600) / 60);
            const secs = seconds % 60;
            $('#elapsed').text(
                `${hours.toString().padStart(2, '0')}:${minutes.toString().padStart(2, '0')}:${secs.toString().padStart(2, '0')}`
            );
        }

        if (data.currentTable) {
            $('#currentTableSection').show();
            $('#currentTable').text(data.currentTable);
            
            const currentTableData = data.tableProgress?.find(t => t.tableName === data.currentTable);
            if (currentTableData) {
                const tablePercent = currentTableData.totalRows > 0 
                    ? Math.round((currentTableData.rowsCopied / currentTableData.totalRows) * 100)
                    : 0;
                $('#tableProgress')
                    .css('width', `${tablePercent}%`)
                    .text(`${tablePercent}%`);
            }
        } else {
            $('#currentTableSection').hide();
        }

        const currentTime = Date.now();
        const timeDiff = (currentTime - this.lastUpdateTime) / 1000;
        if (timeDiff > 0) {
            const rowDiff = data.totalRowsCopied - this.lastRowCount;
            const speed = Math.round(rowDiff / timeDiff);
            $('#speed').text(`${speed.toLocaleString()} rows/s`);
            this.lastRowCount = data.totalRowsCopied;
            this.lastUpdateTime = currentTime;
        }

        if (data.tableProgress && data.tableProgress.length > 0) {
            this.updateTableProgress(data.tableProgress);
        }

        if (data.logs && data.logs.length > 0) {
            this.updateLogs(data.logs);
        }
    }

    updateTableProgress(tableProgress) {
        const $container = $('#tableProgressTable tbody');
        
        if ($container.length === 0) return;

        let html = '';
        tableProgress.forEach(table => {
            const statusBadge = this.getStatusBadge(table.status);
            const progressPercent = table.totalRows > 0 
                ? Math.round((table.rowsCopied / table.totalRows) * 100)
                : 0;
            
            html += `<tr>
                <td>${this.escapeHtml(table.tableName)}</td>
                <td>${statusBadge}</td>
                <td>
                    <div class="progress" style="height: 20px;">
                        <div class="progress-bar ${this.getProgressBarClass(table.status)}" 
                             role="progressbar" style="width: ${progressPercent}%">
                            ${progressPercent}%
                        </div>
                    </div>
                </td>
                <td>${table.rowsCopied.toLocaleString()} / ${table.totalRows.toLocaleString()}</td>
                <td>${table.errorCount > 0 ? `<span class="badge bg-danger">${table.errorCount}</span>` : '-'}</td>
            </tr>`;
        });

        $container.html(html);
    }

    getStatusBadge(status) {
        const badges = {
            'Completed': '<span class="badge bg-success">Completed</span>',
            'Running': '<span class="badge bg-info">Running</span>',
            'Pending': '<span class="badge bg-secondary">Pending</span>'
        };
        return badges[status] || `<span class="badge bg-secondary">${status}</span>`;
    }

    getProgressBarClass(status) {
        switch(status) {
            case 'Completed': return 'bg-success';
            case 'Running': return 'progress-bar-striped progress-bar-animated bg-info';
            case 'Pending': return 'bg-secondary';
            default: return 'bg-secondary';
        }
    }

    updateLogs(logs) {
        const $logContainer = $('#activityLog');
        let logHtml = '';

        logs.forEach(log => {
            const levelClass = this.getLogLevelClass(log.level);
            const timestamp = new Date(log.timestamp).toLocaleTimeString();
            logHtml += `<div class="log-entry ${levelClass}">
                [${timestamp}] [${log.level}] ${this.escapeHtml(log.message)}
            </div>`;
        });

        $logContainer.html(logHtml);
        $logContainer.scrollTop($logContainer[0].scrollHeight);
    }

    getLogLevelClass(level) {
        switch(level.toLowerCase()) {
            case 'error': return 'text-danger';
            case 'warning': return 'text-warning';
            case 'info': return 'text-info';
            default: return 'text-muted';
        }
    }

    escapeHtml(text) {
        const div = document.createElement('div');
        div.textContent = text;
        return div.innerHTML;
    }

    showCompletionButtons() {
        $('#completionButtons').show();
        
        const status = $('#status').text().toLowerCase();
        if (status === 'completed') {
            $('#overallProgress')
                .removeClass('progress-bar-striped progress-bar-animated')
                .addClass('bg-success');
        } else {
            $('#overallProgress')
                .removeClass('progress-bar-striped progress-bar-animated')
                .addClass('bg-danger');
        }
    }
}
