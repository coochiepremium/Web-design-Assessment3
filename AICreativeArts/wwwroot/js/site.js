/* ==========================================================================
   AI Unmasked - site.js
   All JavaScript for the site lives in this external file (no inline or
   internal scripts). Each feature checks that its elements exist first, so
   the file can be loaded safely on every page.

   Contents
     helpers ............ ready, each, createElement, bindHover
     initToolTiles ...... home: mouse-over captions on the case tiles
     initRevealOnView ... home + Artist Shield: blocks appear when fully visible
     initUploadTiles .... home: gallery images fade in on mouse over
     initImageRepeats ... AI Images list: repeating image band on phones
     initImageFallbacks . any uploaded image whose file is missing
     initAnchorHighlight  AI Images list: highlight the block in the URL hash
     initUploadForm ..... Create/Edit: preview + file checks + double-submit guard
     initToasts ......... status messages hide themselves
     initQuiz ........... Artist Shield: quiz marking and score
     initChecklist ...... Artist Shield: protection steps saved in localStorage
   ========================================================================== */
(function () {
    'use strict';

    var root = document.documentElement;
    root.classList.add('js');

    var reduceMotion = window.matchMedia && window.matchMedia('(prefers-reduced-motion: reduce)').matches;

    function ready(callback) {
        if (document.readyState === 'loading') {
            document.addEventListener('DOMContentLoaded', callback);
        } else {
            callback();
        }
    }

    function each(selector, callback, scope) {
        Array.prototype.forEach.call((scope || document).querySelectorAll(selector), callback);
    }

    function createElement(tag, className, text) {
        var element = document.createElement(tag);
        if (className) {
            element.className = className;
        }
        if (text) {
            element.textContent = text;
        }
        return element;
    }

    /* Toggle a class on mouse over / mouse out (and keyboard focus / blur). */
    function bindHover(element, className) {
        element.addEventListener('mouseover', function () {
            element.classList.add(className);
        });
        element.addEventListener('mouseout', function (event) {
            if (!element.contains(event.relatedTarget)) {
                element.classList.remove(className);
            }
        });
        element.addEventListener('focus', function () {
            element.classList.add(className);
        });
        element.addEventListener('blur', function () {
            element.classList.remove(className);
        });
    }

    /* ---------------------------------------------------------------------
       Home: each case tile shows its researched text on mouse over.
       The caption elements are produced here from data-* attributes.
       --------------------------------------------------------------------- */
    function initToolTiles() {
        each('.tool-tile[data-hover-text]', function (tile) {
            var overlay = createElement('span', 'tool-overlay');
            overlay.setAttribute('aria-hidden', 'true');
            overlay.appendChild(createElement('span', 'tool-overlay-title', tile.getAttribute('data-hover-title')));
            overlay.appendChild(createElement('span', 'tool-overlay-text', tile.getAttribute('data-hover-text')));
            overlay.appendChild(createElement('span', 'tool-overlay-source', tile.getAttribute('data-hover-source')));
            tile.appendChild(overlay);
            bindHover(tile, 'is-hovered');
        });
    }

    /* ---------------------------------------------------------------------
       A block appears only while it is fully inside the viewport and
       disappears again when it is not (brief). Blocks taller than the screen
       (small phones) are shown once they fill most of it. Elements marked
       .reveal-once stay visible after they first appear.
       --------------------------------------------------------------------- */
    function initRevealOnView() {
        var blocks = document.querySelectorAll('.reveal-on-view');
        if (!blocks.length) {
            return;
        }

        if (reduceMotion || !('IntersectionObserver' in window)) {
            each('.reveal-on-view', function (block) {
                block.classList.add('is-visible');
            });
            return;
        }

        var observer = new IntersectionObserver(function (entries) {
            var viewportHeight = window.innerHeight || root.clientHeight;
            entries.forEach(function (entry) {
                var block = entry.target;
                var fullyVisible = entry.intersectionRatio >= 0.98;
                var fillsViewport = entry.boundingClientRect.height > viewportHeight * 0.9 &&
                    entry.intersectionRect.height >= viewportHeight * 0.6;

                if (block.classList.contains('reveal-once')) {
                    if (entry.intersectionRatio >= 0.4 || fillsViewport) {
                        block.classList.add('is-visible');
                        observer.unobserve(block);
                    }
                    return;
                }

                block.classList.toggle('is-visible', fullyVisible || fillsViewport);
            });
        }, { threshold: [0, 0.25, 0.4, 0.5, 0.75, 0.9, 0.98, 1] });

        Array.prototype.forEach.call(blocks, function (block) {
            observer.observe(block);
        });
    }

    /* ---------------------------------------------------------------------
       Home: uploaded image tiles. The <img> is produced here and fades in
       over 2 seconds on mouse over (CSS transition).
       --------------------------------------------------------------------- */
    function initUploadTiles() {
        each('.upload-tile[data-image]', function (tile) {
            var source = tile.getAttribute('data-image');
            if (!source) {
                return;
            }
            var image = createElement('img', 'upload-tile-image');
            image.alt = '';
            image.loading = 'lazy';
            image.setAttribute('aria-hidden', 'true');
            image.addEventListener('error', function () {
                // Missing file: keep the generator label instead of a broken image.
                image.remove();
            });
            image.src = source;
            tile.appendChild(image);
            bindHover(tile, 'show-image');
        });
    }

    /* ---------------------------------------------------------------------
       AI Images list: on narrow screens the picture repeats across a band.
       Two decorative copies are added and shown only by the mobile CSS.
       --------------------------------------------------------------------- */
    function initImageRepeats() {
        each('.image-media .image-photo', function (photo) {
            for (var i = 0; i < 2; i++) {
                var copy = photo.cloneNode(false);
                copy.classList.add('image-photo--repeat');
                copy.alt = '';
                copy.removeAttribute('id');
                copy.setAttribute('aria-hidden', 'true');
                photo.parentNode.appendChild(copy);
            }
        });
    }

    /* ---------------------------------------------------------------------
       Uploaded images marked data-image-fallback are replaced by a friendly
       placeholder (built here) if their file cannot be loaded.
       --------------------------------------------------------------------- */
    function replaceWithPlaceholder(image) {
        if (!image.parentNode) {
            return;
        }
        if (image.classList.contains('image-photo--repeat')) {
            image.remove();
            return;
        }
        var placeholder = createElement('span', 'image-slot image-slot--broken ' + image.className.replace('image-photo', ''));
        placeholder.setAttribute('role', 'img');
        placeholder.setAttribute('aria-label', 'Image unavailable');
        placeholder.appendChild(createElement('span', 'image-slot-badge', 'Image unavailable'));
        placeholder.appendChild(createElement('span', 'image-slot-text', 'The uploaded file could not be found. An admin can upload it again from the Edit page.'));
        image.parentNode.replaceChild(placeholder, image);
    }

    function initImageFallbacks() {
        each('img[data-image-fallback]', function (image) {
            image.addEventListener('error', function () {
                replaceWithPlaceholder(image);
            });
            if (image.complete && image.naturalWidth === 0 && image.getAttribute('src') !== null) {
                replaceWithPlaceholder(image);
            }
        });
    }

    /* ---------------------------------------------------------------------
       AI Images list: arriving via #image-N (after Like / Edit, or from a
       home page tile) brings that block to the top and highlights it.
       --------------------------------------------------------------------- */
    function initAnchorHighlight() {
        var id = /^#image-\d+$/.test(window.location.hash) ? window.location.hash.slice(1) : null;
        var target = id ? document.getElementById(id) : null;
        if (!target) {
            return;
        }
        target.classList.add('is-highlighted');
        window.addEventListener('load', function () {
            target.scrollIntoView({ block: 'start', behavior: 'auto' });
        });
    }

    /* ---------------------------------------------------------------------
       Create / Edit: live preview of the chosen file, client-side type and
       size checks, and protection against double submission.
       --------------------------------------------------------------------- */
    function initUploadForm() {
        var input = document.querySelector('input[type="file"][data-preview]');
        if (!input) {
            return;
        }

        var figure = document.querySelector(input.getAttribute('data-preview'));
        var caption = figure ? figure.querySelector('figcaption') : null;
        var chosen = null;

        // The live preview image is produced here, the first time a file is chosen.
        function previewImage() {
            if (!chosen && figure) {
                chosen = createElement('img', 'image-preview-img image-preview-chosen');
                chosen.alt = 'Preview of the image you chose';
                figure.insertBefore(chosen, caption);
            }
            return chosen;
        }
        var defaultCaption = caption ? caption.getAttribute('data-default-caption') : '';
        var message = document.querySelector('[data-valmsg-for="' + input.name + '"]');
        var maxBytes = parseInt(input.getAttribute('data-max-bytes'), 10) || 5 * 1024 * 1024;
        var allowed = /\.(jpe?g|png|gif|webp)$/i;
        var objectUrl = null;

        function showError(text) {
            if (message) {
                message.textContent = text;
                message.classList.toggle('field-validation-error', !!text);
                message.classList.toggle('field-validation-valid', !text);
            }
            input.classList.toggle('input-validation-error', !!text);
        }

        function resetPreview() {
            if (objectUrl) {
                URL.revokeObjectURL(objectUrl);
                objectUrl = null;
            }
            if (chosen) {
                chosen.remove();
                chosen = null;
            }
            if (figure) {
                figure.classList.remove('is-previewing');
            }
            if (caption) {
                caption.textContent = defaultCaption;
            }
        }

        input.addEventListener('change', function () {
            var file = input.files && input.files[0];
            showError('');
            if (!file) {
                resetPreview();
                return;
            }
            if (!allowed.test(file.name)) {
                input.value = '';
                resetPreview();
                showError('Only JPG, PNG, GIF or WEBP images can be uploaded.');
                return;
            }
            if (file.size > maxBytes) {
                input.value = '';
                resetPreview();
                showError('That image is ' + (file.size / 1048576).toFixed(1) + ' MB. Please choose one smaller than ' +
                    Math.round(maxBytes / 1048576) + ' MB.');
                return;
            }
            if (objectUrl) {
                URL.revokeObjectURL(objectUrl);
            }
            objectUrl = URL.createObjectURL(file);
            if (previewImage()) {
                chosen.src = objectUrl;
            }
            if (figure) {
                figure.classList.add('is-previewing');
            }
            if (caption) {
                caption.textContent = 'New image: ' + file.name;
            }
        });

        var form = input.form;
        var submit = form ? form.querySelector('[type="submit"]') : null;
        if (form && submit) {
            form.addEventListener('submit', function () {
                var $ = window.jQuery;
                if ($ && $.fn.valid && !$(form).valid()) {
                    return;
                }
                submit.classList.add('is-busy');
                submit.setAttribute('aria-busy', 'true');
                window.setTimeout(function () {
                    submit.disabled = true;
                }, 0);
            });

            // Re-enable the button if the browser restores this page from its back/forward cache.
            window.addEventListener('pageshow', function () {
                submit.disabled = false;
                submit.classList.remove('is-busy');
                submit.removeAttribute('aria-busy');
            });
        }
    }

    /* ---------------------------------------------------------------------
       Status messages (uploaded, updated, deleted...) hide themselves.
       --------------------------------------------------------------------- */
    function initToasts() {
        if (!window.bootstrap || !window.bootstrap.Toast) {
            return;
        }
        each('.site-toast', function (element) {
            window.bootstrap.Toast.getOrCreateInstance(element, { delay: 7000 }).show();
        });
    }

    /* ---------------------------------------------------------------------
       Artist Shield: mark the quiz, explain each answer and show a score.
       The correct option's index is stored in each fieldset's data-correct.
       --------------------------------------------------------------------- */
    function initQuiz() {
        var form = document.getElementById('quiz-form');
        if (!form) {
            return;
        }
        var score = document.getElementById('quiz-score');
        var questions = form.querySelectorAll('.quiz-question');

        function clearMarks() {
            Array.prototype.forEach.call(questions, function (question) {
                question.classList.remove('is-correct', 'is-wrong', 'is-unanswered');
                each('.quiz-option', function (option) {
                    option.classList.remove('is-correct', 'is-wrong');
                }, question);
                question.querySelector('.quiz-feedback').hidden = true;
            });
            score.textContent = '';
        }

        form.addEventListener('submit', function (event) {
            event.preventDefault();
            clearMarks();

            var right = 0;
            var unanswered = 0;
            Array.prototype.forEach.call(questions, function (question) {
                var correct = parseInt(question.getAttribute('data-correct'), 10);
                var options = question.querySelectorAll('.quiz-option');
                var picked = question.querySelector('input:checked');
                var result = question.querySelector('.quiz-feedback-result');

                options[correct].classList.add('is-correct');
                if (!picked) {
                    unanswered++;
                    question.classList.add('is-unanswered');
                    result.textContent = 'Not answered.';
                } else if (parseInt(picked.value, 10) === correct) {
                    right++;
                    question.classList.add('is-correct');
                    result.textContent = 'Correct!';
                } else {
                    question.classList.add('is-wrong');
                    picked.closest('.quiz-option').classList.add('is-wrong');
                    result.textContent = 'Not quite.';
                }
                question.querySelector('.quiz-feedback').hidden = false;
            });

            var total = questions.length;
            var verdict = right === total ? 'Outstanding: you know exactly what is at stake.'
                : right >= total / 2 ? 'Good effort. Read the explanations to fill the gaps.'
                    : 'The harm is easy to miss. The explanations below show what really happened.';
            score.textContent = 'You scored ' + right + ' out of ' + total + '. ' + verdict +
                (unanswered ? ' (' + unanswered + ' unanswered)' : '');
        });

        form.addEventListener('reset', function () {
            window.setTimeout(clearMarks, 0);
        });
    }

    /* ---------------------------------------------------------------------
       Artist Shield: protection checklist, remembered in this browser.
       Storage can be blocked (private mode), so every access is guarded.
       --------------------------------------------------------------------- */
    var CHECKLIST_KEY = 'aiunmasked.shieldSteps';

    function readSteps() {
        try {
            var value = JSON.parse(window.localStorage.getItem(CHECKLIST_KEY) || '[]');
            return Array.isArray(value) ? value : [];
        } catch (e) {
            return [];
        }
    }

    function writeSteps(steps) {
        try {
            window.localStorage.setItem(CHECKLIST_KEY, JSON.stringify(steps));
        } catch (e) {
            // Progress simply is not remembered when storage is unavailable.
        }
    }

    function initChecklist() {
        var progress = document.getElementById('shield-progress');
        if (!progress) {
            return;
        }
        var text = document.getElementById('shield-progress-text');
        var boxes = document.querySelectorAll('.shield-check');
        var saved = readSteps();

        function update() {
            var done = [];
            Array.prototype.forEach.call(boxes, function (box) {
                box.closest('.shield-step').classList.toggle('is-done', box.checked);
                if (box.checked) {
                    done.push(box.getAttribute('data-step'));
                }
            });
            progress.value = done.length;
            text.textContent = done.length + ' of ' + boxes.length + ' steps done' +
                (done.length === boxes.length ? ' - your art is much better protected.' : '');
            return done;
        }

        Array.prototype.forEach.call(boxes, function (box) {
            box.checked = saved.indexOf(box.getAttribute('data-step')) !== -1;
            box.addEventListener('change', function () {
                writeSteps(update());
            });
        });
        update();

        document.getElementById('shield-reset').addEventListener('click', function () {
            Array.prototype.forEach.call(boxes, function (box) {
                box.checked = false;
            });
            writeSteps(update());
        });
    }

    ready(function () {
        initToolTiles();
        initRevealOnView();
        initUploadTiles();
        initImageRepeats();
        initImageFallbacks();
        initAnchorHighlight();
        initUploadForm();
        initToasts();
        initQuiz();
        initChecklist();
    });
})();
