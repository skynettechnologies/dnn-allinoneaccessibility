'use strict';
/*
 * All in One Accessibility - DNN Persona Bar settings panel.
 * Copyright (c) Skynet Technologies USA LLC. Licensed under the MIT License.
 *
 * Port of the PHP settings page (index.php). Server calls go through the DNN Persona Bar
 * services framework to /API/PersonaBar/AllInOneAccessibility/{GetSettings|SaveSettings}.
 */
define(['jquery'], function ($) {
    var ICON_BASE = 'https://www.skynettechnologies.com/sites/default/files/';
    var ICON_TYPE_COUNT = 29;
    var ICON_SIZES = [
        { value: 'aioa-big-icon', label: 'Big', px: 75 },
        { value: 'aioa-medium-icon', label: 'Medium', px: 65 },
        { value: 'aioa-default-icon', label: 'Default', px: 55 },
        { value: 'aioa-small-icon', label: 'Small', px: 45 },
        { value: 'aioa-extra-small-icon', label: 'Extra Small', px: 35 }
    ];
    var DEFAULTS = {
        widget_position: 'bottom_right',
        widget_color_code: '#420083',
        widget_icon_type: 'aioa-icon-type-1',
        widget_icon_size: 'aioa-default-icon',
        widget_size: 0,
        widget_icon_size_custom: '',
        widget_position_top: 0,
        widget_position_bottom: 0,
        widget_position_left: 0,
        widget_position_right: 0,
        is_widget_custom_size: 0,
        is_widget_custom_position: 0,
        is_free_widget: 1
    };

    var utility = null;
    var $panel = null;
    var saving = false;

    function $q(selector) {
        return $panel.find(selector);
    }

    function toInt(value, fallback) {
        var n = parseInt(value, 10);
        return isNaN(n) ? fallback : n;
    }

    function setStatus(text, isError) {
        var $status = $q('#aioa-status');
        $status.text(text || '').toggleClass('aioa-status-error', !!isError).toggle(!!text);
    }

    function apiCall(method, action, data, success, failure) {
        utility.sf.moduleRoot = 'personaBar';
        utility.sf.controller = 'AllInOneAccessibility';
        utility.sf[method](action, data, success, failure);
    }

    function errorMessage(xhr, fallback) {
        if (xhr && xhr.responseJSON && xhr.responseJSON.message) {
            return xhr.responseJSON.message;
        }
        if (xhr && xhr.responseText) {
            try {
                var parsed = JSON.parse(xhr.responseText);
                if (parsed && (parsed.message || parsed.Message)) {
                    return parsed.message || parsed.Message;
                }
            } catch (e) { /* not JSON */ }
        }
        return fallback;
    }

    /* ---------- building the option grids ---------- */

    function buildIconTypes() {
        var html = '';
        for (var i = 1; i <= ICON_TYPE_COUNT; i++) {
            var value = 'aioa-icon-type-' + i;
            var id = 'aioa-type-' + i;
            html += '<div class="aioa-icon-option">' +
                '<input type="radio" id="' + id + '" name="aioa_icon_type" value="' + value + '"' + (i === 1 ? ' checked' : '') + ' />' +
                '<label for="' + id + '">' +
                '<img src="' + ICON_BASE + value + '.svg" width="65" height="65" alt="" />' +
                '<span class="aioa-visually-hidden">Type ' + i + '</span>' +
                '</label></div>';
        }
        $q('#aioa-icon-types').html(html);
    }

    function buildIconSizes() {
        var html = '';
        $.each(ICON_SIZES, function (index, size) {
            var id = 'aioa-size-' + size.value;
            html += '<div class="aioa-icon-option">' +
                '<input type="radio" id="' + id + '" name="aioa_icon_size" value="' + size.value + '"' + (size.value === DEFAULTS.widget_icon_size ? ' checked' : '') + ' />' +
                '<label for="' + id + '">' +
                '<img class="aioa-size-preview" src="' + ICON_BASE + DEFAULTS.widget_icon_type + '.svg" width="' + size.px + '" height="' + size.px + '" alt="" />' +
                '<span class="aioa-visually-hidden">' + size.label + '</span>' +
                '</label></div>';
        });
        $q('#aioa-icon-sizes').html(html);
    }

    function updateSizePreviews(iconType) {
        $q('.aioa-size-preview').attr('src', ICON_BASE + iconType + '.svg');
    }

    /* ---------- switches ---------- */

    function applyCustomSize(on) {
        $q('#aioa-custom-size-switch').prop('checked', on);
        $q('#aioa-custom-size').toggleClass('aioa-hide', !on);
        $q('#aioa-icon-size-wrapper').toggleClass('aioa-hide', on);
    }

    function applyCustomPosition(on) {
        $q('#aioa-custom-position-switch').prop('checked', on);
        $q('#aioa-custom-position').toggleClass('aioa-hide', !on);
        $q('#aioa-preset-position').toggleClass('aioa-hide', on);
    }

    /* ---------- populate (port of populateSettings) ---------- */

    function populate(result) {
        var s = $.extend({}, DEFAULTS);
        if (result) {
            $.each(DEFAULTS, function (key) {
                if (result[key] !== undefined && result[key] !== null && result[key] !== '') {
                    s[key] = result[key];
                }
            });
        }

        $q('#aioa-colorcode').val(s.widget_color_code);

        $q('input[name="aioa_position"]').filter(function () { return this.value === s.widget_position; }).prop('checked', true);
        $q('input[name="aioa_widget_size"]').filter(function () { return this.value === String(toInt(s.widget_size, 0)); }).prop('checked', true);
        $q('input[name="aioa_icon_type"]').filter(function () { return this.value === s.widget_icon_type; }).prop('checked', true);
        $q('input[name="aioa_icon_size"]').filter(function () { return this.value === s.widget_icon_size; }).prop('checked', true);
        updateSizePreviews(s.widget_icon_type);

        $q('#aioa-icon-size-custom').val(s.widget_icon_size_custom);

        // Custom position: X (right / left)
        var right = toInt(s.widget_position_right, 0), left = toInt(s.widget_position_left, 0);
        if (right > 0) {
            $q('#aioa-position-x').val(right); $q('#aioa-position-x-dir').val('cust-pos-to-the-right');
        } else if (left > 0) {
            $q('#aioa-position-x').val(left); $q('#aioa-position-x-dir').val('cust-pos-to-the-left');
        } else {
            $q('#aioa-position-x').val(0); $q('#aioa-position-x-dir').val('cust-pos-to-the-right');
        }

        // Custom position: Y (bottom / top)
        var bottom = toInt(s.widget_position_bottom, 0), top = toInt(s.widget_position_top, 0);
        if (bottom > 0) {
            $q('#aioa-position-y').val(bottom); $q('#aioa-position-y-dir').val('cust-pos-to-the-lower');
        } else if (top > 0) {
            $q('#aioa-position-y').val(top); $q('#aioa-position-y-dir').val('cust-pos-to-the-upper');
        } else {
            $q('#aioa-position-y').val(0); $q('#aioa-position-y-dir').val('cust-pos-to-the-lower');
        }

        applyCustomSize(toInt(s.is_widget_custom_size, 0) === 1);
        applyCustomPosition(toInt(s.is_widget_custom_position, 0) === 1);

        // Free plan banner: visible for free accounts only.
        $q('#aioa-free-plan').toggle(toInt(s.is_free_widget, 1) === 1);
    }

    function loadSettings() {
        setStatus('Loading settings…');
        $q('#aioa-form').addClass('aioa-busy');
        apiCall('get', 'GetSettings', {}, function (response) {
            $q('#aioa-form').removeClass('aioa-busy');
            populate(response && response.data);
            if (response && response.success === false && response.message) {
                setStatus(response.message, true);
            } else {
                setStatus('');
            }
        }, function (xhr) {
            $q('#aioa-form').removeClass('aioa-busy');
            populate(null);
            setStatus(errorMessage(xhr, 'Could not load the settings. Default values are shown.'), true);
        });
    }

    /* ---------- save (port of the #submit click handler) ---------- */

    function clamp(value, min, max) {
        var n = toInt(value, 0);
        return Math.min(Math.max(n, min), max);
    }

    function collect() {
        var isCustomSize = $q('#aioa-custom-size-switch').is(':checked') ? 1 : 0;
        var isCustomPosition = $q('#aioa-custom-position-switch').is(':checked') ? 1 : 0;
        var customSizeRaw = $.trim($q('#aioa-icon-size-custom').val());

        var x = clamp($q('#aioa-position-x').val(), 0, 250);
        var y = clamp($q('#aioa-position-y').val(), 0, 250);
        var xDir = $q('#aioa-position-x-dir').val();
        var yDir = $q('#aioa-position-y-dir').val();

        return {
            widget_position: $q('input[name="aioa_position"]:checked').val() || DEFAULTS.widget_position,
            is_widget_custom_position: isCustomPosition,
            is_widget_custom_size: isCustomSize,
            widget_color_code: $.trim($q('#aioa-colorcode').val()),
            widget_icon_type: $q('input[name="aioa_icon_type"]:checked').val() || DEFAULTS.widget_icon_type,
            widget_icon_size: $q('input[name="aioa_icon_size"]:checked').val() || DEFAULTS.widget_icon_size,
            widget_size: toInt($q('input[name="aioa_widget_size"]:checked').val(), 0),
            widget_icon_size_custom: customSizeRaw === '' ? null : toInt(customSizeRaw, null),
            widget_position_right: xDir === 'cust-pos-to-the-right' ? x : null,
            widget_position_left: xDir === 'cust-pos-to-the-left' ? x : null,
            widget_position_bottom: yDir === 'cust-pos-to-the-lower' ? y : null,
            widget_position_top: yDir === 'cust-pos-to-the-upper' ? y : null
        };
    }

    function validate(params) {
        if (!/^#?([0-9a-fA-F]{3}|[0-9a-fA-F]{6})$/.test(params.widget_color_code)) {
            return 'Please enter a valid hex color code, for example FF5733.';
        }
        if (params.is_widget_custom_size === 1) {
            if (params.widget_icon_size_custom === null || params.widget_icon_size_custom < 20) {
                return 'Please enter Select exact icon size minimum 20';
            }
            if (params.widget_icon_size_custom > 150) {
                return 'Please enter Select exact icon size maximum 150';
            }
        }
        return null;
    }

    function save() {
        if (saving) {
            return;
        }

        var params = collect();
        var problem = validate(params);
        if (problem) {
            utility.notifyError(problem);
            return;
        }

        saving = true;
        var $btn = $q('#aioa-save').prop('disabled', true).text('Saving…');

        apiCall('post', 'SaveSettings', params, function (response) {
            saving = false;
            $btn.prop('disabled', false).text('Save Changes');
            if (response && response.success) {
                utility.notify(response.message || 'Settings saved successfully!');
                loadSettings();
            } else {
                utility.notifyError((response && response.message) || 'Something went wrong.');
            }
        }, function (xhr) {
            saving = false;
            $btn.prop('disabled', false).text('Save Changes');
            utility.notifyError(errorMessage(xhr, 'Something went wrong.'));
        });
    }

    /* ---------- events ---------- */

    function bindEvents() {
        $q('#aioa-custom-size-switch').on('change', function () {
            applyCustomSize(this.checked);
        });
        $q('#aioa-custom-position-switch').on('change', function () {
            applyCustomPosition(this.checked);
        });
        $panel.on('change', 'input[name="aioa_icon_type"]', function () {
            updateSizePreviews(this.value);
        });
        $q('#aioa-position-x, #aioa-position-y').on('input', function () {
            if (this.value !== '') {
                this.value = clamp(this.value, 0, 250);
            }
        });
        $q('#aioa-save').on('click', save);
    }

    return {
        init: function (wrapper, util, params, callback) {
            utility = util;
            $panel = wrapper;
            buildIconTypes();
            buildIconSizes();
            bindEvents();
            loadSettings();

            if (typeof callback === 'function') {
                callback();
            }
        },

        load: function (params, callback) {
            // Called each time the panel is opened again: refresh from the server.
            loadSettings();

            if (typeof callback === 'function') {
                callback();
            }
        },

        initMobile: function (wrapper, util, params, callback) {
            this.init(wrapper, util, params, callback);
        },

        loadMobile: function (params, callback) {
            this.load(params, callback);
        }
    };
});
