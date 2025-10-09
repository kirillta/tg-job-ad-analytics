(function(){
    'use strict';

    function parseJson(id, fallback){
        var el=document.getElementById(id); if(!el) return fallback;
        try { return JSON.parse(el.textContent || 'null') || fallback; } catch(e){ return fallback; }
    }

    var initialLastMonth = parseJson('stack-comparison-data', []);
    var initialYearGroups = parseJson('stack-comparison-years', []);

    var salaryData = window.salaryData || null;
    var hasSalaryData = !!(salaryData && salaryData.byStack && salaryData.stacks);

    var lastMonth = initialLastMonth;
    if (hasSalaryData) {
        lastMonth = salaryData.stacks.map(function(stack) {
            var stackData = salaryData.byStack[stack.name];
            if (!stackData) return null;
            return {
                name: stack.name,
                label: stack.name,
                count: stack.jobCount,
                median: stackData.aggregates.medianSalary,
                mean: stackData.aggregates.meanSalary,
                p10: stackData.aggregates.percentiles.p10,
                p25: stackData.aggregates.percentiles.p25,
                p50: stackData.aggregates.percentiles.p50,
                p75: stackData.aggregates.percentiles.p75,
                p90: stackData.aggregates.percentiles.p90,
                _share: stack.percentage
            };
        }).filter(Boolean);
    }

    var yearly = initialYearGroups;
    var data = lastMonth;
    var currentLevel = '';
    var showPercentiles = false;

    var filterInput = document.getElementById('stack-filter');
    var pills = document.getElementById('stack-pills');
    var tbody = document.getElementById('stack-rows');
    var tableHeader = document.getElementById('table-header-row');
    var sortSel = document.getElementById('sort-mode');
    var periodSel = document.getElementById('period-mode');
    var yearSel = document.getElementById('year-select');
    var levelFilter = document.getElementById('level-filter');
    var percentilesCheckbox = document.getElementById('show-percentiles');
    var btnAll = document.getElementById('select-all');
    var btnNone = document.getElementById('select-none');
    var btnTop = document.getElementById('top-5');
    var chartCtxEl = document.getElementById('stack-chart');
    if(!chartCtxEl) return; // abort if section not rendered
    var chartCtx = chartCtxEl.getContext('2d');
    var chartInstance = null;
    var selected = new Set(lastMonth.map(function(x){ return x.name; }));

    function hsvToRgb(h, s, v){ var c=v*s; var x=c*(1-Math.abs((h/60)%2-1)); var m=v-c; var r=0,g=0,b=0; if(h<60){r=c;g=x;} else if(h<120){r=x;g=c;} else if(h<180){g=c;b=x;} else if(h<240){g=x;b=c;} else if(h<300){r=x;b=c;} else {r=c;b=x;} return {r:Math.round((r+m)*255),g:Math.round((g+m)*255),b:Math.round((b+m)*255)}; }
    function rgbToHex(r,g,b){return '#'+[r,g,b].map(function(x){var h=x.toString(16);return h.length===1?'0'+h:h;}).join('');}
    function nameColor(name){ var h=0; for(var i=0;i<name.length;i++){h=(h*31 + name.charCodeAt(i))>>>0;} h=h%360; var rgb=hsvToRgb(h,0.65,0.65); return rgbToHex(rgb.r,rgb.g,rgb.b); }
    function computeShare(rows){ var total = data.reduce(function(acc,x){ return acc + x.count; }, 0) || 1; return rows.map(function(x){ var share = total? (x.count/total*100) : 0; x._share = share; return x; }); }
    function updateTableHeaders() {
        if (showPercentiles && hasSalaryData) {
            tableHeader.innerHTML = '<th class="border border-gray-200 px-4 py-2 text-left">Stack</th>' +
                                    '<th class="border border-gray-200 px-4 py-2 text-right">Count</th>' +
                                    '<th class="border border-gray-200 px-4 py-2 text-right">P10</th>' +
                                    '<th class="border border-gray-200 px-4 py-2 text-right">P25</th>' +
                                    '<th class="border border-gray-200 px-4 py-2 text-right">Median</th>' +
                                    '<th class="border border-gray-200 px-4 py-2 text-right">P75</th>' +
                                    '<th class="border border-gray-200 px-4 py-2 text-right">P90</th>' +
                                    '<th class="border border-gray-200 px-4 py-2 text-right">Share</th>';
        } else {
            tableHeader.innerHTML = '<th class="border border-gray-200 px-4 py-2 text-left">Stack</th>' +
                                    '<th class="border border-gray-200 px-4 py-2 text-right">Count</th>' +
                                    '<th class="border border-gray-200 px-4 py-2 text-right">P25</th>' +
                                    '<th class="border border-gray-200 px-4 py-2 text-right">Median</th>' +
                                    '<th class="border border-gray-200 px-4 py-2 text-right">P75</th>' +
                                    '<th class="border border-gray-200 px-4 py-2 text-right">Share</th>';
        }
    }
    function renderPills(items){ pills.innerHTML=''; items.forEach(function(x){ var color=nameColor(x.name); var el=document.createElement('button'); el.className='px-2 py-1 text-xs rounded-md border flex items-center gap-1'; el.style.borderColor=color; el.style.backgroundColor= selected.has(x.name)?color:'transparent'; el.style.color= selected.has(x.name)? '#fff' : color; el.textContent=x.label||x.name; el.addEventListener('click', function(){ if(selected.has(x.name)) selected.delete(x.name); else selected.add(x.name); render(); }); pills.appendChild(el); }); }
    function fmt(v){ return (typeof v==='number' && !isNaN(v)) ? (v%1===0? v.toLocaleString() : v.toLocaleString(undefined,{maximumFractionDigits:2})) : '—'; }
    function renderRows(items){ tbody.innerHTML=''; computeShare(items).forEach(function(x){ if(!selected.has(x.name)) return; var color=nameColor(x.name); var tr=document.createElement('tr'); if (showPercentiles && hasSalaryData) { tr.innerHTML = '<td class="border border-gray-200 px-4 py-2"><span class="inline-block w-2 h-2 rounded-full mr-2" style="background:'+color+'"></span>'+ (x.label||x.name) +'</td>' + '<td class="border border-gray-200 px-4 py-2 text-right">'+x.count.toLocaleString()+'</td>' + '<td class="border border-gray-200 px-4 py-2 text-right">'+fmt(x.p10)+'</td>' + '<td class="border border-gray-200 px-4 py-2 text-right">'+fmt(x.p25)+'</td>' + '<td class="border border-gray-200 px-4 py-2 text-right">'+fmt(x.median)+'</td>' + '<td class="border border-gray-200 px-4 py-2 text-right">'+fmt(x.p75)+'</td>' + '<td class="border border-gray-200 px-4 py-2 text-right">'+fmt(x.p90)+'</td>' + '<td class="border border-gray-200 px-4 py-2 text-right">'+x._share.toFixed(1)+'%</td>'; } else { tr.innerHTML = '<td class="border border-gray-200 px-4 py-2"><span class="inline-block w-2 h-2 rounded-full mr-2" style="background:'+color+'"></span>'+ (x.label||x.name) +'</td>' + '<td class="border border-gray-200 px-4 py-2 text-right">'+x.count.toLocaleString()+'</td>' + '<td class="border border-gray-200 px-4 py-2 text-right">'+fmt(x.p25)+'</td>' + '<td class="border border-gray-200 px-4 py-2 text-right">'+fmt(x.median)+'</td>' + '<td class="border border-gray-200 px-4 py-2 text-right">'+fmt(x.p75)+'</td>' + '<td class="border border-gray-200 px-4 py-2 text-right">'+x._share.toFixed(1)+'%</td>'; } tbody.appendChild(tr); }); }
    function buildBarChartData(rows){ var labels = []; var dataPoints = []; var bg = []; rows.forEach(function(x){ if(!selected.has(x.name)) return; labels.push(x.label||x.name); dataPoints.push(x.median||0); bg.push(nameColor(x.name)); }); return { labels: labels, datasets: [{ label: 'Median', data: dataPoints, backgroundColor: bg }] }; }
    function ensureChart(type, chartData){ if(chartInstance){ chartInstance.destroy(); } chartInstance = new Chart(chartCtx, { type: type, data: chartData, options: { responsive: true, maintainAspectRatio: false, plugins: { legend: { display: false } }, scales: { y: { beginAtZero: true } } } }); }
    function getDataForLevel(level) { if (!level || !hasSalaryData || !salaryData.yearlyStats || !salaryData.yearlyStats.byLevel || !salaryData.yearlyStats.byLevel[level]) { return lastMonth; } var levelStats = salaryData.yearlyStats.byLevel[level]; var years = Object.keys(levelStats.medianByYear || {}).sort(); if (years.length === 0) return lastMonth; return lastMonth; }
    function refreshYearOptions() { yearSel.innerHTML = ''; var years = yearly.map(function(y){ return y.year; }); years.forEach(function(y){ var opt = document.createElement('option'); opt.value = String(y); opt.textContent = String(y); yearSel.appendChild(opt); }); if (years.length) { yearSel.value = String(years[years.length - 1]); loadYear(years[years.length - 1]); yearSel.classList.remove('hidden'); } }
    function loadYear(year) { var yr = parseInt(year, 10); var group = yearly.find(function(g){return g.year===yr;}); if(!group){ data = []; } else { data = group.items || []; } }
    function render(){ var filter=(filterInput.value||'').toLowerCase(); var rows=data.filter(function(x){ return x.name.toLowerCase().indexOf(filter)>=0 || (x.label||'').toLowerCase().indexOf(filter)>=0; }); var mode=sortSel.value; if(mode==='median_desc') rows.sort(function(a,b){return (b.median||0)-(a.median||0)}); else if(mode==='count_desc') rows.sort(function(a,b){return (b.count||0)-(a.count||0)}); else if(mode==='share_desc') rows.sort(function(a,b){return (b._share||0)-(a._share||0)}); updateTableHeaders(); renderPills(rows); renderRows(rows); ensureChart('bar', buildBarChartData(rows)); try{ localStorage.setItem('stack_selected', JSON.stringify(Array.from(selected))); }catch(e){} }

    periodSel.addEventListener('change', function(){ if (periodSel.value === 'year') { refreshYearOptions(); } else { yearSel.classList.add('hidden'); data = getDataForLevel(currentLevel); render(); } });
    yearSel.addEventListener('change', function(){ loadYear(yearSel.value); render(); });
    levelFilter.addEventListener('change', function(){ currentLevel = this.value; data = getDataForLevel(currentLevel); render(); });
    percentilesCheckbox.addEventListener('change', function(){ showPercentiles = this.checked; render(); });
    btnAll.addEventListener('click', function(){ selected = new Set((data.length?data:lastMonth).map(function(x){return x.name;})); render(); });
    btnNone.addEventListener('click', function(){ selected = new Set(); render(); });
    btnTop.addEventListener('click', function(){ var src=(data.length?data:lastMonth).slice().sort(function(a,b){return (b.median||0)-(a.median||0)}).slice(0,5).map(function(x){return x.name}); selected = new Set(src); render(); });
    filterInput.addEventListener('input', render);
    sortSel.addEventListener('change', render);

    render();
})();
