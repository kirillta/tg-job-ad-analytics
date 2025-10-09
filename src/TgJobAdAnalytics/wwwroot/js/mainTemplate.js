(function(){
    'use strict';

    // Parse salary data JSON blob into window.salaryData
    var salaryScript = document.getElementById('salary-data');
    if (salaryScript) {
        try { window.salaryData = JSON.parse(salaryScript.textContent || 'null'); }
        catch(e){ console.warn('Failed parsing salary data JSON', e); window.salaryData = null; }
    }

    function bindLocaleSelect(select){
        if(!select || select.__localeBound) return;
        select.__localeBound = true;
        select.addEventListener('change', function(){
            var v = this.value; if(v){ window.location.href = v; }
        });
    }

    function initLocaleSwitcher(){
        var sel = document.getElementById('locale-select');
        if(sel){ bindLocaleSelect(sel); return true; }
        return false;
    }

    if(!initLocaleSwitcher()){
        // Wait for DOM ready if script loaded in <head>
        if(document.readyState === 'loading'){
            document.addEventListener('DOMContentLoaded', initLocaleSwitcher);
        } else {
            // Fallback: observe for late injected select
            var mo = new MutationObserver(function(){ if(initLocaleSwitcher()){ mo.disconnect(); } });
            mo.observe(document.documentElement || document.body, { childList:true, subtree:true });
        }
    }
})();
