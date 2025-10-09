/**
 * Client-side stack filtering for static HTML salary reports
 * Enables users to filter salary statistics by technology stack without server-side calls
 */
(function() {
    'use strict';

    // Check if salary data is available
    if (typeof window.salaryData === 'undefined' || !window.salaryData) {
        console.warn('Stack filtering: salary data not available');
        return;
    }

    class StackFilter {
        constructor(data) {
            this.allData = data;
            this.currentFilter = {
                mode: 'all', // 'all' | 'include' | 'exclude'
                stacks: []
            };
            this.charts = {};
            
            this.initializeUI();
            this.populateStackSelector();
            this.loadFilterFromURL();
            this.applyFilter();
        }

        /**
         * Initialize UI event listeners
         */
        initializeUI() {
            // Filter mode radio buttons
            const modeRadios = document.querySelectorAll('input[name="stackFilterMode"]');
            modeRadios.forEach(radio => {
                radio.addEventListener('change', (e) => this.handleModeChange(e.target.value));
            });

            // Stack selector
            const selector = document.getElementById('stack-filter-select');
            if (selector) {
                selector.addEventListener('change', () => {
                    const applyBtn = document.getElementById('apply-stack-filter');
                    if (applyBtn) applyBtn.disabled = false;
                });
            }

            // Apply button
            const applyBtn = document.getElementById('apply-stack-filter');
            if (applyBtn) {
                applyBtn.addEventListener('click', () => this.applyFilter());
            }

            // Reset button
            const resetBtn = document.getElementById('reset-stack-filter');
            if (resetBtn) {
                resetBtn.addEventListener('click', () => this.resetFilter());
            }
        }

        /**
         * Populate stack selector with available stacks
         */
        populateStackSelector() {
            const selector = document.getElementById('stack-filter-select');
            if (!selector) return;

            const stacks = this.allData.stacks || [];
            stacks.sort((a, b) => b.jobCount - a.jobCount);

            selector.innerHTML = stacks.map(stack => 
                `<option value="${this.escapeHtml(stack.id)}">
                    ${this.escapeHtml(stack.name)} (${stack.jobCount.toLocaleString()} jobs, ${stack.percentage.toFixed(1)}%)
                </option>`
            ).join('');
        }

        /**
         * Handle filter mode change
         */
        handleModeChange(mode) {
            this.currentFilter.mode = mode;
            const selector = document.getElementById('stack-filter-select');
            const applyBtn = document.getElementById('apply-stack-filter');

            if (mode === 'all') {
                if (selector) selector.disabled = true;
                if (applyBtn) applyBtn.disabled = true;
                this.applyFilter();
            } else {
                if (selector) selector.disabled = false;
                if (applyBtn) applyBtn.disabled = false;
            }
        }

        /**
         * Apply current filter settings
         */
        applyFilter() {
            const selector = document.getElementById('stack-filter-select');
            if (selector) {
                const selectedOptions = Array.from(selector.selectedOptions);
                this.currentFilter.stacks = selectedOptions.map(opt => opt.value);
            }

            const filteredData = this.getFilteredData();
            this.updateStatisticsDisplay(filteredData);
            this.updateFilterStatus();
            this.updateURL();
        }

        /**
         * Reset filter to show all data
         */
        resetFilter() {
            const allRadio = document.querySelector('input[name="stackFilterMode"][value="all"]');
            if (allRadio) allRadio.checked = true;
            
            this.currentFilter = { mode: 'all', stacks: [] };
            
            const selector = document.getElementById('stack-filter-select');
            if (selector) selector.selectedIndex = -1;
            
            this.applyFilter();
        }

        /**
         * Get filtered dataset based on current filter settings
         */
        getFilteredData() {
            const { mode, stacks } = this.currentFilter;

            if (mode === 'all' || stacks.length === 0) {
                return this.allData.global;
            }

            if (mode === 'include') {
                if (stacks.length === 1) {
                    return this.allData.byStack[stacks[0]] || this.createEmptyDataset();
                } else {
                    return this.combineStackData(stacks);
                }
            }

            if (mode === 'exclude') {
                const allStackIds = this.allData.stacks.map(s => s.id);
                const includedStacks = allStackIds.filter(id => !stacks.includes(id));
                return this.combineStackData(includedStacks);
            }

            return this.allData.global;
        }

        /**
         * Combine data from multiple stacks
         */
        combineStackData(stackIds) {
            const stackDatasets = stackIds
                .map(id => this.allData.byStack[id])
                .filter(d => d);

            if (stackDatasets.length === 0) {
                return this.createEmptyDataset();
            }

            return {
                distribution: this.combineDistributions(stackDatasets),
                aggregates: this.combineAggregates(stackDatasets)
            };
        }

        /**
         * Combine distribution data from multiple stacks
         */
        combineDistributions(datasets) {
            const distributionByBucket = new Map();

            datasets.forEach(dataset => {
                if (!dataset.distribution) return;
                dataset.distribution.forEach(bucket => {
                    const current = distributionByBucket.get(bucket.bucket) || { count: 0 };
                    current.count += bucket.count;
                    distributionByBucket.set(bucket.bucket, current);
                });
            });

            const totalCount = Array.from(distributionByBucket.values())
                .reduce((sum, b) => sum + b.count, 0);

            return Array.from(distributionByBucket.entries())
                .map(([bucket, data]) => ({
                    bucket,
                    count: data.count,
                    percentage: totalCount > 0 ? (data.count / totalCount) * 100 : 0
                }));
        }

        /**
         * Combine aggregate statistics from multiple stacks
         */
        combineAggregates(datasets) {
            const totalJobs = datasets.reduce((sum, d) => sum + (d.aggregates?.totalJobs || 0), 0);

            if (totalJobs === 0) {
                return {
                    totalJobs: 0,
                    medianSalary: 0,
                    meanSalary: 0,
                    percentiles: {}
                };
            }

            const medianSalary = Math.round(
                datasets.reduce((sum, d) => 
                    sum + ((d.aggregates?.medianSalary || 0) * (d.aggregates?.totalJobs || 0)), 0
                ) / totalJobs
            );

            const meanSalary = Math.round(
                datasets.reduce((sum, d) => 
                    sum + ((d.aggregates?.meanSalary || 0) * (d.aggregates?.totalJobs || 0)), 0
                ) / totalJobs
            );

            const percentiles = {};
            ['p10', 'p25', 'p50', 'p75', 'p90'].forEach(key => {
                percentiles[key] = Math.round(
                    datasets.reduce((sum, d) => 
                        sum + ((d.aggregates?.percentiles?.[key] || 0) * (d.aggregates?.totalJobs || 0)), 0
                    ) / totalJobs
                );
            });

            return {
                totalJobs,
                medianSalary,
                meanSalary,
                percentiles
            };
        }

        /**
         * Update statistics display in the page
         */
        updateStatisticsDisplay(data) {
            console.log('Filtered statistics:', data);

            const totalJobsEl = document.getElementById('filtered-total-jobs');
            if (totalJobsEl && data.aggregates) {
                totalJobsEl.textContent = data.aggregates.totalJobs.toLocaleString();
            }

            const medianSalaryEl = document.getElementById('filtered-median-salary');
            if (medianSalaryEl && data.aggregates) {
                medianSalaryEl.textContent = this.formatCurrency(data.aggregates.medianSalary);
            }
        }

        /**
         * Update filter status display
         */
        updateFilterStatus() {
            const statusDiv = document.getElementById('stack-filter-status');
            if (!statusDiv) return;

            if (this.currentFilter.mode === 'all') {
                statusDiv.style.display = 'none';
                return;
            }

            const stackNames = this.currentFilter.stacks.map(id => {
                const stack = this.allData.stacks.find(s => s.id === id);
                return stack ? stack.name : id;
            });

            const modeText = this.currentFilter.mode === 'include' ? 'Showing only' : 'Excluding';
            const descSpan = document.getElementById('stack-filter-description');
            if (descSpan) {
                descSpan.textContent = `${modeText}: ${stackNames.join(', ')}`;
            }

            const filteredData = this.getFilteredData();
            const countSpan = document.getElementById('filtered-job-count');
            if (countSpan && filteredData.aggregates) {
                countSpan.textContent = filteredData.aggregates.totalJobs.toLocaleString();
            }

            statusDiv.style.display = 'block';
        }

        /**
         * Update URL hash for shareable filters
         */
        updateURL() {
            const { mode, stacks } = this.currentFilter;

            if (mode === 'all') {
                if (window.location.hash.includes('stackFilter')) {
                    window.location.hash = '';
                }
                return;
            }

            const params = new URLSearchParams();
            params.set('stackFilter', mode);
            if (stacks.length > 0) {
                params.set('stacks', stacks.join(','));
            }

            window.location.hash = params.toString();
        }

        /**
         * Load filter settings from URL hash
         */
        loadFilterFromURL() {
            const hash = window.location.hash.substring(1);
            if (!hash) return;

            const params = new URLSearchParams(hash);
            const mode = params.get('stackFilter');
            const stacks = params.get('stacks');

            if (mode && ['include', 'exclude'].includes(mode)) {
                this.currentFilter.mode = mode;
                const modeRadio = document.querySelector(`input[name="stackFilterMode"][value="${mode}"]`);
                if (modeRadio) modeRadio.checked = true;

                if (stacks) {
                    this.currentFilter.stacks = stacks.split(',');
                    const selector = document.getElementById('stack-filter-select');
                    if (selector) {
                        Array.from(selector.options).forEach(opt => {
                            opt.selected = this.currentFilter.stacks.includes(opt.value);
                        });
                    }
                }
            }
        }

        /**
         * Create empty dataset for edge cases
         */
        createEmptyDataset() {
            return {
                distribution: [],
                aggregates: {
                    totalJobs: 0,
                    medianSalary: 0,
                    meanSalary: 0,
                    percentiles: {}
                }
            };
        }

        /**
         * Format currency for display
         */
        formatCurrency(amount) {
            return new Intl.NumberFormat('en-US', {
                style: 'currency',
                currency: 'USD',
                minimumFractionDigits: 0,
                maximumFractionDigits: 0
            }).format(amount);
        }

        /**
         * Escape HTML to prevent XSS
         */
        escapeHtml(text) {
            const div = document.createElement('div');
            div.textContent = text;
            return div.innerHTML;
        }
    }

    // Initialize when DOM is ready
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', () => {
            window.stackFilter = new StackFilter(window.salaryData);
        });
    } else {
        window.stackFilter = new StackFilter(window.salaryData);
    }
})();
