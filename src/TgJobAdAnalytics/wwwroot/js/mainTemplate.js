(function(){
    'use strict';
    // Parse salary data JSON blob into window.salaryData
    var salaryScript = document.getElementById('salary-data');
    if (salaryScript) {
        try { window.salaryData = JSON.parse(salaryScript.textContent || 'null'); }
        catch(e){ console.warn('Failed parsing salary data JSON', e); window.salaryData = null; }
    }

    // Locale switcher
    var sel = document.getElementById('locale-select');
    if(sel){
        sel.addEventListener('change', function(){
            var v = this.value; if(v) window.location.href = v;
        });
    }
})();
