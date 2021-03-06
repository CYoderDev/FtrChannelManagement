(function (global, factory) {
	typeof exports === 'object' && typeof module !== 'undefined' ? factory(exports, require('@angular/core'), require('@angular/common'), require('rxjs/Observable'), require('rxjs/add/observable/fromEvent'), require('rxjs/add/operator/filter'), require('rxjs/Subject'), require('rxjs/add/observable/merge'), require('rxjs/add/observable/of'), require('rxjs/add/observable/zip'), require('rxjs/add/operator/do'), require('rxjs/add/operator/map'), require('rxjs/add/operator/share'), require('rxjs/add/operator/take'), require('rxjs/add/operator/toPromise')) :
	typeof define === 'function' && define.amd ? define(['exports', '@angular/core', '@angular/common', 'rxjs/Observable', 'rxjs/add/observable/fromEvent', 'rxjs/add/operator/filter', 'rxjs/Subject', 'rxjs/add/observable/merge', 'rxjs/add/observable/of', 'rxjs/add/observable/zip', 'rxjs/add/operator/do', 'rxjs/add/operator/map', 'rxjs/add/operator/share', 'rxjs/add/operator/take', 'rxjs/add/operator/toPromise'], factory) :
	(factory((global['ng2-bs3-modal'] = {}),global.ng.core,global.ng.common,global.Rx,global.Rx.Observable,global.Rx.Observable.prototype,global.Rx));
}(this, (function (exports,core,common,Observable,fromEvent,filter,Subject) { 'use strict';

(function (BsModalHideType) {
    BsModalHideType[BsModalHideType["Close"] = 0] = "Close";
    BsModalHideType[BsModalHideType["Dismiss"] = 1] = "Dismiss";
    BsModalHideType[BsModalHideType["Backdrop"] = 2] = "Backdrop";
    BsModalHideType[BsModalHideType["Keyboard"] = 3] = "Keyboard";
    BsModalHideType[BsModalHideType["RouteChange"] = 4] = "RouteChange";
    BsModalHideType[BsModalHideType["Destroy"] = 5] = "Destroy";
})(exports.BsModalHideType || (exports.BsModalHideType = {}));

var BsModalSize = /** @class */ (function () {
    function BsModalSize() {
    }
    BsModalSize.isValidSize = function (size) {
        return size && (size === BsModalSize.Small || size === BsModalSize.Large);
    };
    BsModalSize.Small = 'sm';
    BsModalSize.Large = 'lg';
    return BsModalSize;
}());

var EVENT_SUFFIX = 'ng2-bs3-modal';
var KEYUP_EVENT_NAME = "keyup." + EVENT_SUFFIX;
var CLICK_EVENT_NAME = "click." + EVENT_SUFFIX;
var SHOW_EVENT_NAME = "show.bs.modal." + EVENT_SUFFIX;
var BsModalService = /** @class */ (function () {
    function BsModalService() {
        var _this = this;
        this.modals = [];
        this.$body = jQuery(document.body);
        this.onBackdropClose$ = Observable.Observable.fromEvent(this.$body, CLICK_EVENT_NAME)
            .filter(function (e) { return jQuery(e.target).is('.modal'); })
            .map(function () { return exports.BsModalHideType.Backdrop; })
            .share();
        this.onKeyboardClose$ = Observable.Observable.fromEvent(this.$body, KEYUP_EVENT_NAME)
            .filter(function (e) { return e.which === 27; })
            .map(function () { return exports.BsModalHideType.Keyboard; })
            .share();
        this.onModalStack$ = Observable.Observable.fromEvent(this.$body, SHOW_EVENT_NAME)
            .do(function () {
            var zIndex = 1040 + (10 * $('.modal:visible').length);
            $(_this).css('z-index', zIndex);
            setTimeout(function () {
                $('.modal-backdrop').not('.modal-stack').css('z-index', zIndex - 1).addClass('modal-stack');
            }, 0);
        })
            .share();
    }
    BsModalService.prototype.add = function (modal) {
        this.modals.push(modal);
    };
    BsModalService.prototype.remove = function (modal) {
        var index = this.modals.indexOf(modal);
        if (index > -1)
            this.modals.splice(index, 1);
    };
    BsModalService.prototype.focusNext = function () {
        var visible = this.modals.filter(function (m) { return m.visible; });
        if (visible.length) {
            this.$body.addClass('modal-open');
            visible[visible.length - 1].focus();
        }
    };
    BsModalService.prototype.dismissAll = function () {
        return Promise.all(this.modals.map(function (m) {
            return m.dismiss();
        }));
    };
    BsModalService.decorators = [
        { type: core.Injectable },
    ];
    /** @nocollapse */
    BsModalService.ctorParameters = function () { return []; };
    return BsModalService;
}());

var EVENT_SUFFIX$1 = 'ng2-bs3-modal';
var SHOW_EVENT_NAME$1 = "show.bs.modal." + EVENT_SUFFIX$1;
var SHOWN_EVENT_NAME = "shown.bs.modal." + EVENT_SUFFIX$1;
var HIDE_EVENT_NAME = "hide.bs.modal." + EVENT_SUFFIX$1;
var HIDDEN_EVENT_NAME = "hidden.bs.modal." + EVENT_SUFFIX$1;
var LOADED_EVENT_NAME = "loaded.bs.modal." + EVENT_SUFFIX$1;
var DATA_KEY = 'bs.modal';
var BsModalComponent = /** @class */ (function () {
    function BsModalComponent(element, service, zone) {
        var _this = this;
        this.element = element;
        this.service = service;
        this.zone = zone;
        this.overrideSize = null;
        this.onInternalClose$ = new Subject.Subject();
        this.subscriptions = [];
        this.visible = false;
        this.animation = true;
        this.backdrop = true;
        this.keyboard = true;
        this.onShow = new core.EventEmitter();
        this.onOpen = new core.EventEmitter();
        this.onHide = new core.EventEmitter();
        this.onClose = new core.EventEmitter();
        this.onDismiss = new core.EventEmitter();
        this.onLoaded = new core.EventEmitter();
        this.setVisible = function (isVisible) {
            return function () {
                _this.visible = isVisible;
            };
        };
        this.setOptions = function (options) {
            var backdrop = options.backdrop;
            if (typeof backdrop === 'string' && backdrop !== 'static')
                backdrop = true;
            if (options.backdrop !== undefined)
                _this.options.backdrop = backdrop;
            if (options.keyboard !== undefined)
                _this.options.keyboard = options.keyboard;
        };
        this.service.add(this);
        this.init();
    }
    Object.defineProperty(BsModalComponent.prototype, "options", {
        get: function () {
            if (!this.$modal)
                this.init();
            return this.$modal.data(DATA_KEY).options;
        },
        enumerable: true,
        configurable: true
    });
    Object.defineProperty(BsModalComponent.prototype, "fadeClass", {
        get: function () { return this.animation; },
        enumerable: true,
        configurable: true
    });
    Object.defineProperty(BsModalComponent.prototype, "modalClass", {
        get: function () { return true; },
        enumerable: true,
        configurable: true
    });
    Object.defineProperty(BsModalComponent.prototype, "roleAttr", {
        get: function () { return 'dialog'; },
        enumerable: true,
        configurable: true
    });
    Object.defineProperty(BsModalComponent.prototype, "tabindexAttr", {
        get: function () { return '-1'; },
        enumerable: true,
        configurable: true
    });
    BsModalComponent.prototype.ngOnInit = function () {
        this.wireUpEventEmitters();
    };
    BsModalComponent.prototype.ngAfterViewInit = function () {
        this.$dialog = this.$modal.find('.modal-dialog');
    };
    BsModalComponent.prototype.ngOnChanges = function () {
        this.setOptions({
            backdrop: this.backdrop,
            keyboard: this.keyboard
        });
    };
    BsModalComponent.prototype.ngOnDestroy = function () {
        this.onInternalClose$.next(exports.BsModalHideType.Destroy);
        return this.destroy();
    };
    BsModalComponent.prototype.triggerTransitionEnd = function () {
        this.$dialog.trigger('transitionend');
    };
    BsModalComponent.prototype.focus = function () {
        this.$modal.trigger('focus');
    };
    BsModalComponent.prototype.routerCanDeactivate = function () {
        this.onInternalClose$.next(exports.BsModalHideType.RouteChange);
        return this.destroy();
    };
    BsModalComponent.prototype.open = function (size) {
        this.overrideSize = null;
        if (BsModalSize.isValidSize(size))
            this.overrideSize = size;
        return this.show().toPromise();
    };
    BsModalComponent.prototype.close = function (value) {
        var _this = this;
        this.onInternalClose$.next(exports.BsModalHideType.Close);
        return this.hide()
            .do(function () { return _this.onClose.emit(value); })
            .toPromise()
            .then(function () { return value; });
    };
    BsModalComponent.prototype.dismiss = function () {
        this.onInternalClose$.next(exports.BsModalHideType.Dismiss);
        return this.hide().toPromise();
    };
    BsModalComponent.prototype.getCssClasses = function () {
        var classes = [];
        if (this.isSmall()) {
            classes.push('modal-sm');
        }
        if (this.isLarge()) {
            classes.push('modal-lg');
        }
        if (this.cssClass) {
            classes.push(this.cssClass);
        }
        return classes.join(' ');
    };
    BsModalComponent.prototype.isSmall = function () {
        return this.overrideSize !== BsModalSize.Large
            && this.size === BsModalSize.Small
            || this.overrideSize === BsModalSize.Small;
    };
    BsModalComponent.prototype.isLarge = function () {
        return this.overrideSize !== BsModalSize.Small
            && this.size === BsModalSize.Large
            || this.overrideSize === BsModalSize.Large;
    };
    BsModalComponent.prototype.show = function () {
        var _this = this;
        if (this.visible)
            return Observable.Observable.of(null);
        return Observable.Observable.create(function (o) {
            _this.onShown$.take(1).subscribe(function (next) {
                o.next(next);
                o.complete();
            });
            _this.transitionFix();
            _this.$modal.modal('show');
        });
    };
    BsModalComponent.prototype.transitionFix = function () {
        var _this = this;
        // Fix for shown.bs.modal not firing when .fade is present
        // https://github.com/twbs/bootstrap/issues/11793
        if (this.animation) {
            this.$dialog.one('transitionend', function () {
                _this.$modal.trigger('focus').trigger(SHOWN_EVENT_NAME);
            });
        }
    };
    BsModalComponent.prototype.hide = function () {
        var _this = this;
        if (!this.visible)
            return Observable.Observable.of(null);
        return Observable.Observable.create(function (o) {
            _this.onHidden$.take(1).subscribe(function (next) {
                o.next(next);
                o.complete();
            });
            _this.$modal.modal('hide');
        });
    };
    BsModalComponent.prototype.init = function () {
        var _this = this;
        this.$modal = jQuery(this.element.nativeElement);
        this.$modal.appendTo(document.body);
        this.$modal.modal({
            show: false
        });
        this.onShowEvent$ = Observable.Observable.fromEvent(this.$modal, SHOW_EVENT_NAME$1);
        this.onShownEvent$ = Observable.Observable.fromEvent(this.$modal, SHOWN_EVENT_NAME);
        this.onHideEvent$ = Observable.Observable.fromEvent(this.$modal, HIDE_EVENT_NAME);
        this.onHiddenEvent$ = Observable.Observable.fromEvent(this.$modal, HIDDEN_EVENT_NAME);
        this.onLoadedEvent$ = Observable.Observable.fromEvent(this.$modal, LOADED_EVENT_NAME);
        var onClose$ = Observable.Observable
            .merge(this.onInternalClose$, this.service.onBackdropClose$, this.service.onKeyboardClose$)
            .filter(function () { return _this.visible; });
        this.onHide$ = Observable.Observable.zip(this.onHideEvent$, onClose$)
            .map(function (x) { return ({ event: x[0], type: x[1] }); });
        this.onHidden$ = Observable.Observable.zip(this.onHiddenEvent$, onClose$)
            .map(function (x) { return x[1]; })
            .do(this.setVisible(false))
            .do(function () { return _this.service.focusNext(); })
            .share();
        this.onShown$ = this.onShownEvent$
            .do(this.setVisible(true))
            .share();
        this.onDismiss$ = this.onHidden$
            .filter(function (x) { return x !== exports.BsModalHideType.Close; });
        // Start watching for events
        (_a = this.subscriptions).push.apply(_a, [
            this.onShown$.subscribe(function () { }),
            this.onHidden$.subscribe(function () { }),
            this.service.onModalStack$.subscribe(function () { })
        ]);
        var _a;
    };
    BsModalComponent.prototype.wireUpEventEmitters = function () {
        this.wireUpEventEmitter(this.onShow, this.onShowEvent$);
        this.wireUpEventEmitter(this.onOpen, this.onShown$);
        this.wireUpEventEmitter(this.onHide, this.onHide$);
        this.wireUpEventEmitter(this.onDismiss, this.onDismiss$);
        this.wireUpEventEmitter(this.onLoaded, this.onLoadedEvent$);
    };
    BsModalComponent.prototype.wireUpEventEmitter = function (emitter, stream$) {
        var _this = this;
        if (emitter.observers.length === 0)
            return;
        var sub = stream$.subscribe(function (next) {
            _this.zone.run(function () {
                emitter.next(next);
            });
        });
        this.subscriptions.push(sub);
    };
    BsModalComponent.prototype.destroy = function () {
        var _this = this;
        return this.hide().do(function () {
            _this.service.remove(_this);
            _this.subscriptions.forEach(function (s) { return s.unsubscribe(); });
            _this.subscriptions = [];
            if (_this.$modal) {
                _this.$modal.data(DATA_KEY, null);
                _this.$modal.remove();
                _this.$modal = null;
            }
        }).toPromise();
    };
    BsModalComponent.decorators = [
        { type: core.Component, args: [{
                    selector: 'bs-modal',
                    template: "\n        <div class=\"modal-dialog\" [ngClass]=\"getCssClasses()\">\n            <div class=\"modal-content\">\n                <ng-content></ng-content>\n            </div>\n        </div>\n    "
                },] },
    ];
    /** @nocollapse */
    BsModalComponent.ctorParameters = function () { return [
        { type: core.ElementRef, },
        { type: BsModalService, },
        { type: core.NgZone, },
    ]; };
    BsModalComponent.propDecorators = {
        'animation': [{ type: core.Input },],
        'backdrop': [{ type: core.Input },],
        'keyboard': [{ type: core.Input },],
        'size': [{ type: core.Input },],
        'cssClass': [{ type: core.Input },],
        'onShow': [{ type: core.Output },],
        'onOpen': [{ type: core.Output },],
        'onHide': [{ type: core.Output },],
        'onClose': [{ type: core.Output },],
        'onDismiss': [{ type: core.Output },],
        'onLoaded': [{ type: core.Output },],
        'fadeClass': [{ type: core.HostBinding, args: ['class.fade',] },],
        'modalClass': [{ type: core.HostBinding, args: ['class.modal',] },],
        'roleAttr': [{ type: core.HostBinding, args: ['attr.role',] },],
        'tabindexAttr': [{ type: core.HostBinding, args: ['attr.tabindex',] },],
    };
    return BsModalComponent;
}());

var BsModalHeaderComponent = /** @class */ (function () {
    function BsModalHeaderComponent(modal) {
        this.modal = modal;
        this.showDismiss = false;
    }
    BsModalHeaderComponent.decorators = [
        { type: core.Component, args: [{
                    selector: 'bs-modal-header',
                    template: "\n        <div class=\"modal-header\">\n            <button *ngIf=\"showDismiss\" type=\"button\" class=\"close\" aria-label=\"Dismiss\" (click)=\"modal.dismiss()\">\n                <span aria-hidden=\"true\">&times;</span>\n            </button>\n            <ng-content></ng-content>\n        </div>\n    "
                },] },
    ];
    /** @nocollapse */
    BsModalHeaderComponent.ctorParameters = function () { return [
        { type: BsModalComponent, },
    ]; };
    BsModalHeaderComponent.propDecorators = {
        'showDismiss': [{ type: core.Input },],
    };
    return BsModalHeaderComponent;
}());

var BsModalBodyComponent = /** @class */ (function () {
    function BsModalBodyComponent() {
    }
    BsModalBodyComponent.decorators = [
        { type: core.Component, args: [{
                    selector: 'bs-modal-body',
                    template: "\n        <div class=\"modal-body\">\n            <ng-content></ng-content>\n        </div>\n    "
                },] },
    ];
    /** @nocollapse */
    BsModalBodyComponent.ctorParameters = function () { return []; };
    return BsModalBodyComponent;
}());

var BsModalFooterComponent = /** @class */ (function () {
    function BsModalFooterComponent(modal) {
        this.modal = modal;
        this.showDefaultButtons = false;
        this.dismissButtonLabel = 'Dismiss';
        this.closeButtonLabel = 'Close';
    }
    BsModalFooterComponent.decorators = [
        { type: core.Component, args: [{
                    selector: 'bs-modal-footer',
                    template: "\n        <div class=\"modal-footer\">\n            <ng-content></ng-content>\n            <button *ngIf=\"showDefaultButtons\" type=\"button\" class=\"btn btn-default\" (click)=\"modal.dismiss()\">\n                {{dismissButtonLabel}}\n            </button>\n            <button *ngIf=\"showDefaultButtons\" type=\"button\" class=\"btn btn-primary\" (click)=\"modal.close()\">\n                {{closeButtonLabel}}\n              </button>\n        </div>\n    "
                },] },
    ];
    /** @nocollapse */
    BsModalFooterComponent.ctorParameters = function () { return [
        { type: BsModalComponent, },
    ]; };
    BsModalFooterComponent.propDecorators = {
        'showDefaultButtons': [{ type: core.Input },],
        'dismissButtonLabel': [{ type: core.Input },],
        'closeButtonLabel': [{ type: core.Input },],
    };
    return BsModalFooterComponent;
}());

var BsAutofocusDirective = /** @class */ (function () {
    function BsAutofocusDirective(el, modal) {
        var _this = this;
        this.el = el;
        this.modal = modal;
        if (modal) {
            this.modal.onOpen.subscribe(function () {
                _this.el.nativeElement.focus();
            });
        }
    }
    BsAutofocusDirective.decorators = [
        { type: core.Directive, args: [{
                    selector: '[autofocus]'
                },] },
    ];
    /** @nocollapse */
    BsAutofocusDirective.ctorParameters = function () { return [
        { type: core.ElementRef, },
        { type: BsModalComponent, decorators: [{ type: core.Optional },] },
    ]; };
    return BsAutofocusDirective;
}());

var BsModalModule = /** @class */ (function () {
    function BsModalModule() {
    }
    BsModalModule.decorators = [
        { type: core.NgModule, args: [{
                    imports: [
                        common.CommonModule
                    ],
                    declarations: [
                        BsModalComponent,
                        BsModalHeaderComponent,
                        BsModalBodyComponent,
                        BsModalFooterComponent,
                        BsAutofocusDirective
                    ],
                    providers: [
                        BsModalService
                    ],
                    exports: [
                        BsModalComponent,
                        BsModalHeaderComponent,
                        BsModalBodyComponent,
                        BsModalFooterComponent,
                        BsAutofocusDirective
                    ]
                },] },
    ];
    /** @nocollapse */
    BsModalModule.ctorParameters = function () { return []; };
    return BsModalModule;
}());

exports.BsModalModule = BsModalModule;
exports.BsModalService = BsModalService;
exports.BsModalComponent = BsModalComponent;
exports.BsModalHeaderComponent = BsModalHeaderComponent;
exports.BsModalBodyComponent = BsModalBodyComponent;
exports.BsModalFooterComponent = BsModalFooterComponent;
exports.BsModalSize = BsModalSize;

Object.defineProperty(exports, '__esModule', { value: true });

})));
