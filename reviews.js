/* Write Review: hover preview and the live label/counter.
   Progressive enhancement - the radios and the 500-character limit are enforced without
   this file, and again on the server by ReviewValidator. */
(function () {
    'use strict';

    var LABELS = {
        '1': '1 (Poor)', '2': '2 (Fair)', '3': '3 (Good)',
        '4': '4 (Very good)', '5': '5 (Excellent)'
    };

    document.addEventListener('DOMContentLoaded', function () {
        var label = document.querySelector('[data-rating-label]');
        var radios = document.querySelectorAll('.star-rating__list input[type="radio"]');

        function show() {
            for (var i = 0; i < radios.length; i++) {
                if (radios[i].checked) {
                    if (label) { label.textContent = LABELS[radios[i].value] || ''; }
                    return;
                }
            }
            if (label) { label.textContent = ''; }
        }

        for (var i = 0; i < radios.length; i++) {
            radios[i].addEventListener('change', show);
        }
        show();

        var box = document.querySelector('[data-counted]');
        var counter = document.querySelector('[data-counter]');
        if (box && counter) {
            var update = function () { counter.textContent = String(box.value.length); };
            box.addEventListener('input', update);
            update();
        }
    });
})();
