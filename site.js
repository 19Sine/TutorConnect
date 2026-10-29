/* Submit feedback shared by every signed-in page.

   Two jobs, both small:

   1. Show that a postback is in progress. Web Forms does a full page load, so without
      this a slow query looks like nothing happened and the user clicks again.

   2. Stop that second click reaching the server. The double-click case is already safe -
      accepting a request twice, or reviewing a booking twice, is refused by the database
      and the serialisable transaction - but refusing it is a worse experience than not
      letting it happen. This is the courtesy; the server is the guarantee.

   Progressive enhancement: without this file every form still submits normally. */
(function () {
    'use strict';

    var BUSY_CLASS = 'is-busy';
    var LOCK_MS = 4000;

    function markBusy(button) {
        if (!button || button.classList.contains(BUSY_CLASS)) { return false; }

        button.classList.add(BUSY_CLASS);
        button.setAttribute('aria-busy', 'true');

        // A link-button has no disabled state that survives the postback, so re-enable
        // after a moment. If the response arrives first the page is replaced anyway.
        window.setTimeout(function () {
            button.classList.remove(BUSY_CLASS);
            button.removeAttribute('aria-busy');
        }, LOCK_MS);

        return true;
    }

    document.addEventListener('DOMContentLoaded', function () {
        var form = document.forms[0];
        if (!form) { return; }

        form.addEventListener('click', function (e) {
            var target = e.target;

            // Walk up: the click may land on text inside the control.
            while (target && target !== form) {
                var isSubmit =
                    (target.tagName === 'INPUT' && (target.type === 'submit' || target.type === 'button')) ||
                    (target.tagName === 'A' && target.getAttribute('href') &&
                     target.getAttribute('href').indexOf('__doPostBack') !== -1);

                if (isSubmit) {
                    // A control that asked for confirmation and was refused must not be
                    // left looking busy.
                    if (!markBusy(target)) { e.preventDefault(); }
                    return;
                }

                target = target.parentNode;
            }
        }, true);

        // Anything already in flight is cleared when the page comes back.
        window.addEventListener('pageshow', function () {
            var busy = document.querySelectorAll('.' + BUSY_CLASS);
            for (var i = 0; i < busy.length; i++) {
                busy[i].classList.remove(BUSY_CLASS);
                busy[i].removeAttribute('aria-busy');
            }
        });
    });
})();
