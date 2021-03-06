import { Component, Input, Output, EventEmitter, ElementRef, HostBinding, NgZone } from '@angular/core';
import { Observable } from 'rxjs/Observable';
import { Subject } from 'rxjs/Subject';
import 'rxjs/add/observable/fromEvent';
import 'rxjs/add/observable/merge';
import 'rxjs/add/observable/of';
import 'rxjs/add/observable/zip';
import 'rxjs/add/operator/do';
import 'rxjs/add/operator/filter';
import 'rxjs/add/operator/map';
import 'rxjs/add/operator/share';
import 'rxjs/add/operator/take';
import 'rxjs/add/operator/toPromise';
import { BsModalHideType, BsModalSize } from './models';
import { BsModalService } from './modal-service';
var EVENT_SUFFIX = 'ng2-bs3-modal';
var SHOW_EVENT_NAME = "show.bs.modal." + EVENT_SUFFIX;
var SHOWN_EVENT_NAME = "shown.bs.modal." + EVENT_SUFFIX;
var HIDE_EVENT_NAME = "hide.bs.modal." + EVENT_SUFFIX;
var HIDDEN_EVENT_NAME = "hidden.bs.modal." + EVENT_SUFFIX;
var LOADED_EVENT_NAME = "loaded.bs.modal." + EVENT_SUFFIX;
var DATA_KEY = 'bs.modal';
var BsModalComponent = /** @class */ (function () {
    function BsModalComponent(element, service, zone) {
        var _this = this;
        this.element = element;
        this.service = service;
        this.zone = zone;
        this.overrideSize = null;
        this.onInternalClose$ = new Subject();
        this.subscriptions = [];
        this.visible = false;
        this.animation = true;
        this.backdrop = true;
        this.keyboard = true;
        this.onShow = new EventEmitter();
        this.onOpen = new EventEmitter();
        this.onHide = new EventEmitter();
        this.onClose = new EventEmitter();
        this.onDismiss = new EventEmitter();
        this.onLoaded = new EventEmitter();
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
        this.onInternalClose$.next(BsModalHideType.Destroy);
        return this.destroy();
    };
    BsModalComponent.prototype.triggerTransitionEnd = function () {
        this.$dialog.trigger('transitionend');
    };
    BsModalComponent.prototype.focus = function () {
        this.$modal.trigger('focus');
    };
    BsModalComponent.prototype.routerCanDeactivate = function () {
        this.onInternalClose$.next(BsModalHideType.RouteChange);
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
        this.onInternalClose$.next(BsModalHideType.Close);
        return this.hide()
            .do(function () { return _this.onClose.emit(value); })
            .toPromise()
            .then(function () { return value; });
    };
    BsModalComponent.prototype.dismiss = function () {
        this.onInternalClose$.next(BsModalHideType.Dismiss);
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
            return Observable.of(null);
        return Observable.create(function (o) {
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
            return Observable.of(null);
        return Observable.create(function (o) {
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
        this.onShowEvent$ = Observable.fromEvent(this.$modal, SHOW_EVENT_NAME);
        this.onShownEvent$ = Observable.fromEvent(this.$modal, SHOWN_EVENT_NAME);
        this.onHideEvent$ = Observable.fromEvent(this.$modal, HIDE_EVENT_NAME);
        this.onHiddenEvent$ = Observable.fromEvent(this.$modal, HIDDEN_EVENT_NAME);
        this.onLoadedEvent$ = Observable.fromEvent(this.$modal, LOADED_EVENT_NAME);
        var onClose$ = Observable
            .merge(this.onInternalClose$, this.service.onBackdropClose$, this.service.onKeyboardClose$)
            .filter(function () { return _this.visible; });
        this.onHide$ = Observable.zip(this.onHideEvent$, onClose$)
            .map(function (x) { return ({ event: x[0], type: x[1] }); });
        this.onHidden$ = Observable.zip(this.onHiddenEvent$, onClose$)
            .map(function (x) { return x[1]; })
            .do(this.setVisible(false))
            .do(function () { return _this.service.focusNext(); })
            .share();
        this.onShown$ = this.onShownEvent$
            .do(this.setVisible(true))
            .share();
        this.onDismiss$ = this.onHidden$
            .filter(function (x) { return x !== BsModalHideType.Close; });
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
        { type: Component, args: [{
                    selector: 'bs-modal',
                    template: "\n        <div class=\"modal-dialog\" [ngClass]=\"getCssClasses()\">\n            <div class=\"modal-content\">\n                <ng-content></ng-content>\n            </div>\n        </div>\n    "
                },] },
    ];
    /** @nocollapse */
    BsModalComponent.ctorParameters = function () { return [
        { type: ElementRef, },
        { type: BsModalService, },
        { type: NgZone, },
    ]; };
    BsModalComponent.propDecorators = {
        'animation': [{ type: Input },],
        'backdrop': [{ type: Input },],
        'keyboard': [{ type: Input },],
        'size': [{ type: Input },],
        'cssClass': [{ type: Input },],
        'onShow': [{ type: Output },],
        'onOpen': [{ type: Output },],
        'onHide': [{ type: Output },],
        'onClose': [{ type: Output },],
        'onDismiss': [{ type: Output },],
        'onLoaded': [{ type: Output },],
        'fadeClass': [{ type: HostBinding, args: ['class.fade',] },],
        'modalClass': [{ type: HostBinding, args: ['class.modal',] },],
        'roleAttr': [{ type: HostBinding, args: ['attr.role',] },],
        'tabindexAttr': [{ type: HostBinding, args: ['attr.tabindex',] },],
    };
    return BsModalComponent;
}());
export { BsModalComponent };
//# sourceMappingURL=modal.js.map