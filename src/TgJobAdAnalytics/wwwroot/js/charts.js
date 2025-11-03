(function(){
    'use strict';

    if (typeof Chart === 'undefined') return;

    function parse(id){ var el=document.getElementById(id); if(!el) return null; try { return JSON.parse(el.textContent||'null'); } catch(e){ console.warn('Parse error', id, e); return null; } }
    function toNumericArray(arr){ return (Array.isArray(arr)?arr:[]).map(function(v){ if (v && typeof v === 'object') { if('y' in v) return Number(v.y); if('value' in v) return Number(v.value);} return Number(v); }); }
    function isYearLabels(labels){ return Array.isArray(labels) && labels.length>0 && labels.every(function(l){ return /^\d{4}$/.test(String(l)); }); }
    function padYearSeries(labels,data){ if(!isYearLabels(labels)) return {labels:labels,data:data}; var years=labels.map(function(l){return parseInt(String(l),10);}); var minY=Math.min.apply(null,years); var maxY=Math.max.apply(null,years); var currentY=(new Date()).getFullYear(); var endY=Math.max(maxY,currentY); var map=new Map(); for(var i=0;i<labels.length;i++){ map.set(String(labels[i]), Number(data[i])); } var paddedLabels=[]; var paddedData=[]; for(var y=minY;y<=endY;y++){ var key=String(y); paddedLabels.push(key); var val=map.has(key)?map.get(key):0; paddedData.push(val);} return {labels:paddedLabels,data:paddedData}; }
    function niceStep(max,tickTarget){ var raw=(max||1)/(tickTarget||6); var pow=Math.pow(10, Math.floor(Math.log10(raw))); var norm=raw/pow; var s= norm<=1?1: norm<=2?2: norm<=5?5:10; return s*pow; }
    function computeYAxis(data){ var nums=toNumericArray(data).filter(Number.isFinite); var maxY=nums.length?Math.max.apply(null,nums):0; var suggested=Math.ceil(maxY*1.05); var step=niceStep(suggested,6); var yMax=Math.ceil((suggested||1)/step)*step; return {step:step,max:yMax}; }
    function weightedMedian(values, weights){ var pairs=values.map(function(v,i){return {v:v,w:weights[i]||0};}).filter(function(p){return Number.isFinite(p.v)&&p.w>0;}).sort(function(a,b){return a.v-b.v;}); var total=pairs.reduce(function(s,p){return s+p.w;},0); if(total===0||pairs.length===0) return NaN; var cum=0; for(var i=0;i<pairs.length;i++){ cum+=pairs[i].w; if(cum>=total/2) return pairs[i].v; } return pairs[pairs.length-1].v; }

    function initChart(container){
        var baseId = container.getAttribute('data-chart-id');
        var metricCode = container.getAttribute('data-metric-code') || '';
        var canvas = document.getElementById(baseId);
        if(!canvas) return;
        var base = parse(baseId + '-base-data');
        if(!base) return;
        var padded = padYearSeries(base.labels, base.dataset.data);
        base.labels = padded.labels; base.dataset.data = padded.data;
        var yAxis = computeYAxis(base.dataset.data);
        var ctx = canvas.getContext('2d');
        var chart = new Chart(ctx, { type: container.getAttribute('data-chart-type') || 'bar', data: { labels: base.labels, datasets: [{ label: base.dataset.label, data: base.dataset.data, backgroundColor: base.dataset.backgroundColor, borderColor: base.dataset.borderColor, borderWidth: base.dataset.borderWidth, fill: base.dataset.fill, tension: base.dataset.tension }] }, options: { maintainAspectRatio: true, responsive: true, plugins: { legend: { labels: { boxWidth:12, boxHeight:12 } }, tooltip: { callbacks: { title: function(items){ return items && items.length ? String(items[0].label):''; }, label: function(context){ var idx=context.dataIndex; var raw=context.dataset && context.dataset.data ? context.dataset.data[idx] : context.raw; var value=(raw && typeof raw==='object' && 'y' in raw) ? raw.y : raw; var num=Number(value); return Number.isFinite(num)? new Intl.NumberFormat('ru-RU',{maximumFractionDigits:0}).format(num) : String(value); } } } }, scales: { x: { type:'category', offset:true, ticks:{display:false}, grid:{drawBorder:false} }, y: { beginAtZero:true, suggestedMax: yAxis.max, ticks:{display:false}, grid:{drawBorder:false} } } } });

        var variants = parse(baseId + '-variants');
        var preferredLabel = parse(baseId + '-preferred-label');
        var order = parse(baseId + '-variant-order');
        var selector = document.getElementById(baseId + '-variant');
        var stackSelector = document.getElementById(baseId + '-stack');
        var percentilesCheckbox = document.getElementById(baseId + '-show-percentiles');
        var salaryData = window.salaryData || null;
        var hasSalaryData = !!(salaryData && salaryData.yearlyStats && salaryData.yearlyStats.byLevel);
        var hasStackData = !!(salaryData && salaryData.byStack && salaryData.stacks);
        var hasYearlyByStack = !!(salaryData && salaryData.yearlyByStack);

        var currentVariant = null; var currentStack = ''; var showPercentiles = false;

        function aggregateYearMetricFromTrends(pointsByYear) {
            if (/max_years$/i.test(metricCode)) { var out={}; Object.keys(pointsByYear).forEach(function(y){ var arr=pointsByYear[y].map(function(p){return p.median;}).filter(Number.isFinite); out[y]=arr.length?Math.max.apply(null,arr):null; }); return out; }
            if (/min_years$/i.test(metricCode)) { var mn={}; Object.keys(pointsByYear).forEach(function(y){ var arr=pointsByYear[y].map(function(p){return p.median;}).filter(Number.isFinite); mn[y]=arr.length?Math.min.apply(null,arr):null; }); return mn; }
            if (/avg_years$/i.test(metricCode)) { var av={}; Object.keys(pointsByYear).forEach(function(y){ var pts=pointsByYear[y]; var sumW=pts.reduce(function(s,p){return s+(p.count||0);},0); if(!sumW){ av[y]=null; return;} var num=pts.reduce(function(s,p){return s+(p.mean*(p.count||0));},0); av[y]=num/sumW; }); return av; }
            var med={}; Object.keys(pointsByYear).forEach(function(y){ var pts=pointsByYear[y]; var vals=pts.map(function(p){return p.median;}); var w=pts.map(function(p){return p.count||0;}); var wm=weightedMedian(vals,w); med[y]=Number.isFinite(wm)?wm:(vals.length?vals.sort(function(a,b){return a-b;})[Math.floor(vals.length/2)]:null); }); return med; }

        function applyVariant(name){
            currentVariant = name;
            if(!variants || !variants[name]) return;
            var v = variants[name];
            var paddedV = padYearSeries(v.labels, v.dataset.data);
            var vLabels = paddedV.labels; var vData = paddedV.data;
            chart.data.labels = vLabels;
            var ds0 = chart.data.datasets[0];
            ds0.label = v.dataset.label; ds0.data = vData; ds0.backgroundColor = v.dataset.backgroundColor; ds0.borderColor = v.dataset.borderColor; ds0.borderWidth = v.dataset.borderWidth; ds0.fill = v.dataset.fill; ds0.tension = v.dataset.tension;
            if(currentStack && (hasYearlyByStack || hasStackData)) applyStackOverlay(currentStack, currentVariant); else { while(chart.data.datasets.length>1) chart.data.datasets.pop(); }
            var yConf = computeYAxis(vData); chart.options.scales.y.suggestedMax = yConf.max; chart.update();
        }

        function applyStackOverlay(stackName, levelName){
            if(!stackName){ while(chart.data.datasets.length>1) chart.data.datasets.pop(); return; }
            var overlay = null;
            if(hasYearlyByStack && salaryData.yearlyByStack && salaryData.yearlyByStack[stackName]){ var ys = salaryData.yearlyByStack[stackName]; var metricMap=null; if(/max_years$/i.test(metricCode)) metricMap=ys.maximumByYear; else if(/min_years$/i.test(metricCode)) metricMap=ys.minimumByYear; else if(/avg_years$/i.test(metricCode)) metricMap=ys.averageByYear; else metricMap=ys.medianByYear; overlay = chart.data.labels.map(function(lbl){ if(/^\d{4}$/.test(String(lbl))){ var y=String(lbl); var v = metricMap && Object.prototype.hasOwnProperty.call(metricMap,y) ? metricMap[y] : null; return (v==null||Number.isNaN(v))?null:v; } return null; }); }
            if(!overlay){ if(!hasStackData || !salaryData.byStack || !salaryData.byStack[stackName] || !salaryData.byStack[stackName].trends) return; var trends = salaryData.byStack[stackName].trends; var pointsByYear={}; trends.forEach(function(tr){ var year=tr.date.substring(0,4); (pointsByYear[year]||(pointsByYear[year]=[])).push({ median: tr.median, mean: tr.mean, count: tr.count }); }); var metricByYear = aggregateYearMetricFromTrends(pointsByYear); overlay = chart.data.labels.map(function(lbl){ if(/^[0-9]{4}$/.test(String(lbl))){ var y=String(lbl); var v = Object.prototype.hasOwnProperty.call(metricByYear,y)?metricByYear[y]:null; return (v==null||Number.isNaN(v))?null:v; } return null; }); }
            while(chart.data.datasets.length>1) chart.data.datasets.pop();
            chart.data.datasets.push({ label: stackName + ' (' + levelName + ')', data: overlay, backgroundColor:'transparent', borderColor:'rgba(255,99,132,1)', borderWidth:2, borderDash:[5,5], fill:false, tension:0.1, type:'line' });
            var allData = chart.data.datasets.flatMap(function(ds){ return ds.data; }); var yConf=computeYAxis(allData); chart.options.scales.y.suggestedMax = yConf.max; chart.update();
        }

        function orderVariantKeys(variantsObj, orderArr){
            var keys = Object.keys(variantsObj||{});
            if(!Array.isArray(orderArr) || !orderArr.length) return keys;
            var out=[]; orderArr.forEach(function(lbl){ if(keys.indexOf(lbl)>=0) out.push(lbl); });
            keys.forEach(function(k){ if(out.indexOf(k)<0) out.push(k); });
            return out;
        }

        if(variants && selector){
            var keysOrdered = orderVariantKeys(variants, order);
            keysOrdered.forEach(function(k){ var opt=document.createElement('option'); opt.value=k; opt.text=k; selector.appendChild(opt); });
            var preferred = (preferredLabel && keysOrdered.indexOf(preferredLabel)>=0)?preferredLabel:(keysOrdered.indexOf('???')>=0?'???':keysOrdered[0]);
            selector.value = preferred;
            applyVariant(preferred);
            selector.addEventListener('change', function(){ applyVariant(this.value); });
        }
        if(hasStackData && stackSelector){ var stacks = (salaryData && salaryData.stacks)?salaryData.stacks:[]; stacks.forEach(function(stack){ var opt=document.createElement('option'); opt.value=stack.name; opt.text = stack.name + ' (' + stack.jobCount + ')'; stackSelector.appendChild(opt); }); stackSelector.addEventListener('change', function(){ currentStack=this.value; applyVariant(currentVariant|| (selector?selector.value:null)); }); }
        if(percentilesCheckbox){ percentilesCheckbox.addEventListener('change', function(){ /* placeholder for percentile toggle if needed */ }); }
    }

    document.querySelectorAll('.chart-container').forEach(initChart);
})();

// Table toggle logic extracted from templates
(function(){
    'use strict';
    function toggle(btn){ var root = btn.closest('.report-fragment') || document; var table = root.querySelector('table'); if(!table) return; var hidden = table.classList.toggle('hidden'); var labelEl = btn.querySelector('.toggle-label'); var showLabel = btn.getAttribute('data-show-label'); var hideLabel = btn.getAttribute('data-hide-label'); if(labelEl){ labelEl.textContent = hidden ? showLabel : hideLabel; } }
    document.addEventListener('click', function(e){ var btn = e.target.closest('.toggle-table-btn'); if(!btn) return; e.preventDefault(); toggle(btn); });
})();
