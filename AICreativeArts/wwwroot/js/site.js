/* ==========================================================================
   MuseAI - site.js
   All JavaScript for the site lives in this external file (no inline or
   internal scripts). Each feature checks that its elements exist first, so
   the file can be loaded safely on every page.
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
       Home: square tool tiles show researched text on mouse over.
       The caption elements are produced here from data-* attributes.
       --------------------------------------------------------------------- */
    function initToolTiles() {
        each('.tool-tile[data-hover-text]', function (tile) {
            var overlay = createElement('span', 'tool-overlay');
            overlay.setAttribute('aria-hidden', 'true');
            overlay.appendChild(createElement('span', 'tool-overlay-text', tile.getAttribute('data-hover-text')));
            overlay.appendChild(createElement('span', 'tool-overlay-source', tile.getAttribute('data-hover-source')));
            tile.appendChild(overlay);
            bindHover(tile, 'is-hovered');
        });
    }

    /* ---------------------------------------------------------------------
       Home & Prompt Lab: a block appears only while it is fully inside the
       viewport and disappears again when it is not. Blocks taller than the
       screen (small phones) are shown once they fill most of it.
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
                var fullyVisible = entry.intersectionRatio >= 0.98;
                var fillsViewport = entry.boundingClientRect.height > viewportHeight * 0.9 &&
                    entry.intersectionRect.height >= viewportHeight * 0.6;
                var visible = fullyVisible || fillsViewport;
                var block = entry.target;

                if (block.classList.contains('reveal-once')) {
                    if (entry.intersectionRatio >= 0.5 || fillsViewport) {
                        block.classList.add('is-visible');
                        observer.unobserve(block);
                    }
                    return;
                }

                block.classList.toggle('is-visible', visible);
            });
        }, { threshold: [0, 0.25, 0.5, 0.75, 0.9, 0.98, 1] });

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
            var image = createElement('img', 'upload-tile-image');
            image.src = tile.getAttribute('data-image');
            image.alt = '';
            image.loading = 'lazy';
            image.setAttribute('aria-hidden', 'true');
            image.setAttribute('data-fallback', tile.getAttribute('data-fallback') || '');
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
       Any image with data-fallback swaps to a friendly placeholder if its
       file is missing, instead of showing a broken-image icon.
       --------------------------------------------------------------------- */
    function useFallback(image) {
        var fallback = image.getAttribute('data-fallback');
        if (!fallback || image.getAttribute('data-fallback-used')) {
            return;
        }
        image.setAttribute('data-fallback-used', 'true');
        image.src = fallback;
    }

    function initImageFallbacks() {
        each('img[data-fallback]', function (image) {
            image.addEventListener('error', function () {
                useFallback(image);
            });
            if (image.complete && image.naturalWidth === 0 && image.getAttribute('src')) {
                useFallback(image);
            }
        });
    }

    /* ---------------------------------------------------------------------
       AI Images list: arriving via #image-N (after Like / Edit, or from a
       home page tile) brings that block to the top and highlights it.
       --------------------------------------------------------------------- */
    function initAnchorHighlight() {
        var match = /^#image-\d+$/.test(window.location.hash) ? window.location.hash.slice(1) : null;
        var target = match ? document.getElementById(match) : null;
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

        var preview = document.querySelector(input.getAttribute('data-preview'));
        var caption = preview ? preview.parentNode.querySelector('figcaption') : null;
        var defaultCaption = caption ? caption.textContent : '';
        var message = document.querySelector('[data-valmsg-for="' + input.name + '"]');
        var maxBytes = parseInt(input.getAttribute('data-max-bytes'), 10) || 5 * 1024 * 1024;
        var allowed = /\.(jpe?g|png|gif|webp)$/i;
        var objectUrl = null;

        function showError(text) {
            if (message) {
                message.textContent = text;
                message.classList.remove('field-validation-valid');
                message.classList.add('field-validation-error');
            }
            input.classList.toggle('input-validation-error', !!text);
        }

        function resetPreview() {
            if (objectUrl) {
                URL.revokeObjectURL(objectUrl);
                objectUrl = null;
            }
            if (preview) {
                preview.removeAttribute('data-fallback-used');
                preview.src = preview.getAttribute('data-default-src');
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
            if (preview) {
                preview.src = objectUrl;
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
       Prompt Lab: build an AI art prompt from chips, copy / save it, and
       carry it to the upload form.
       --------------------------------------------------------------------- */
    var STORAGE_KEY = 'museai.savedPrompts';

    function readSaved() {
        try {
            var value = JSON.parse(window.localStorage.getItem(STORAGE_KEY) || '[]');
            return Array.isArray(value) ? value.filter(function (item) { return typeof item === 'string'; }) : [];
        } catch (e) {
            return [];
        }
    }

    function writeSaved(list) {
        try {
            window.localStorage.setItem(STORAGE_KEY, JSON.stringify(list));
            return true;
        } catch (e) {
            return false;
        }
    }

    function copyText(text) {
        if (navigator.clipboard && window.isSecureContext) {
            return navigator.clipboard.writeText(text);
        }
        return new Promise(function (resolve, reject) {
            var helper = createElement('textarea', 'visually-hidden');
            helper.value = text;
            helper.setAttribute('readonly', '');
            document.body.appendChild(helper);
            helper.select();
            try {
                if (document.execCommand('copy')) {
                    resolve();
                } else {
                    reject(new Error('copy failed'));
                }
            } catch (e) {
                reject(e);
            } finally {
                document.body.removeChild(helper);
            }
        });
    }

    function initPromptLab() {
        var output = document.getElementById('prompt-output');
        if (!output) {
            return;
        }

        var order = ['style', 'lighting', 'palette', 'mood', 'framing'];
        var labels = ['Empty', 'Basic', 'Basic', 'Good', 'Good', 'Detailed', 'Masterful'];
        var placeholder = output.textContent.trim();
        var custom = document.getElementById('custom-subject');
        var strength = document.getElementById('prompt-strength');
        var words = document.getElementById('prompt-words');
        var copyButton = document.getElementById('prompt-copy');
        var saveButton = document.getElementById('prompt-save');
        var uploadLink = document.getElementById('prompt-upload');
        var feedback = document.getElementById('prompt-feedback');
        var savedList = document.getElementById('saved-prompts');
        var savedEmpty = document.getElementById('saved-empty');
        var steps = document.querySelectorAll('.meter-step');
        var current = '';
        var feedbackTimer = null;

        function selected(group) {
            var chip = document.querySelector('.chip-group[data-group="' + group + '"] .chip[aria-pressed="true"]');
            return chip ? chip.getAttribute('data-value') : '';
        }

        function capitalise(text) {
            return text.charAt(0).toUpperCase() + text.slice(1);
        }

        function say(text) {
            feedback.textContent = text;
            window.clearTimeout(feedbackTimer);
            feedbackTimer = window.setTimeout(function () {
                feedback.textContent = '';
            }, 3500);
        }

        function build() {
            var subject = (custom.value || '').trim() || selected('subject');
            var details = order.map(selected).filter(Boolean);
            var parts = subject ? [subject].concat(details) : details;
            current = parts.length ? capitalise(parts.join(', ')) + '.' : '';

            output.textContent = current || placeholder;
            output.classList.toggle('is-empty', !current);
            output.classList.remove('is-updated');
            if (current && !reduceMotion) {
                void output.offsetWidth;
                output.classList.add('is-updated');
            }

            var count = parts.length;
            Array.prototype.forEach.call(steps, function (step, index) {
                step.classList.toggle('is-filled', index < count);
            });
            strength.textContent = labels[Math.min(count, labels.length - 1)];
            words.textContent = current ? current.split(/\s+/).length : 0;
            copyButton.disabled = !current;
            saveButton.disabled = !current;

            var base = uploadLink.getAttribute('data-base-href');
            uploadLink.href = current ? base + '?prompt=' + encodeURIComponent(current) : base;
        }

        function press(chip, pressed) {
            chip.setAttribute('aria-pressed', pressed ? 'true' : 'false');
        }

        each('.chip-group', function (group) {
            group.addEventListener('click', function (event) {
                var chip = event.target.closest('.chip');
                if (!chip) {
                    return;
                }
                var wasPressed = chip.getAttribute('aria-pressed') === 'true';
                each('.chip', function (other) {
                    press(other, false);
                }, group);
                press(chip, !wasPressed);
                if (group.getAttribute('data-group') === 'subject' && !wasPressed) {
                    custom.value = '';
                }
                build();
            });
        });

        custom.addEventListener('input', function () {
            if (custom.value.trim()) {
                each('.chip-group[data-group="subject"] .chip', function (chip) {
                    press(chip, false);
                });
            }
            build();
        });

        document.getElementById('prompt-random').addEventListener('click', function () {
            custom.value = '';
            each('.chip-group', function (group) {
                var chips = group.querySelectorAll('.chip');
                var pick = Math.floor(Math.random() * chips.length);
                Array.prototype.forEach.call(chips, function (chip, index) {
                    press(chip, index === pick);
                });
            });
            build();
            say('A fresh combination - try another if it does not inspire you!');
        });

        document.getElementById('prompt-clear').addEventListener('click', function () {
            custom.value = '';
            each('.chip', function (chip) {
                press(chip, false);
            });
            build();
            say('Cleared. Start again whenever you are ready.');
        });

        copyButton.addEventListener('click', function () {
            if (!current) {
                return;
            }
            copyText(current).then(function () {
                say('Copied to your clipboard.');
            }, function () {
                say('Copying is blocked in this browser - please select the text and copy it manually.');
            });
        });

        function renderSaved() {
            var list = readSaved();
            savedList.textContent = '';
            list.forEach(function (text, index) {
                var item = createElement('li', 'saved-prompt');
                item.appendChild(createElement('span', 'saved-prompt-text', text));

                var actions = createElement('span', 'saved-prompt-actions');
                var copy = createElement('button', 'btn btn-outline-light btn-sm', 'Copy');
                copy.type = 'button';
                copy.setAttribute('aria-label', 'Copy saved prompt ' + (index + 1));
                copy.addEventListener('click', function () {
                    copyText(text).then(function () {
                        say('Saved prompt copied.');
                    }, function () {
                        say('Copying is blocked in this browser.');
                    });
                });

                var remove = createElement('button', 'btn btn-outline-danger btn-sm', 'Remove');
                remove.type = 'button';
                remove.setAttribute('aria-label', 'Remove saved prompt ' + (index + 1));
                remove.addEventListener('click', function () {
                    var updated = readSaved();
                    updated.splice(index, 1);
                    writeSaved(updated);
                    renderSaved();
                    say('Saved prompt removed.');
                });

                actions.appendChild(copy);
                actions.appendChild(remove);
                item.appendChild(actions);
                savedList.appendChild(item);
            });
            savedEmpty.classList.toggle('is-hidden', list.length > 0);
        }

        saveButton.addEventListener('click', function () {
            if (!current) {
                return;
            }
            var list = readSaved().filter(function (text) {
                return text !== current;
            });
            list.unshift(current);
            if (writeSaved(list.slice(0, 8))) {
                renderSaved();
                say('Prompt saved in this browser.');
            } else {
                say('Saving is not available in this browser (private mode?).');
            }
        });

        renderSaved();
        build();
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
        initPromptLab();
    });
})();
