"use strict";

var __decorate = this && this.__decorate || function (decorators, target, key, desc) {
    var c = arguments.length,
        r = c < 3 ? target : desc === null ? desc = Object.getOwnPropertyDescriptor(target, key) : desc,
        d;
    if (typeof Reflect === "object" && typeof Reflect.decorate === "function") r = Reflect.decorate(decorators, target, key, desc);else for (var i = decorators.length - 1; i >= 0; i--) if (d = decorators[i]) r = (c < 3 ? d(r) : c > 3 ? d(target, key, r) : d(target, key)) || r;
    return c > 3 && r && Object.defineProperty(target, key, r), r;
};
var __metadata = this && this.__metadata || function (k, v) {
    if (typeof Reflect === "object" && typeof Reflect.metadata === "function") return Reflect.metadata(k, v);
};
Object.defineProperty(exports, "__esModule", { value: true });
var core_1 = require("@angular/core");
var _ = require("lodash");
require("rxjs/add/operator/filter");
require("rxjs/add/operator/finally");
var channel_service_1 = require("../Service/channel.service");
var ChannelInfoComponent = function () {
    function ChannelInfoComponent(_channelService) {
        this._channelService = _channelService;
        this.stationchange = new core_1.EventEmitter();
        this.onshow = new core_1.EventEmitter();
        this.showSubmit = false;
        this.isSubmitting = false;
        this.stationLoading = false;
        this.regionLoading = false;
        this.editField = '';
    }
    Object.defineProperty(ChannelInfoComponent.prototype, "channel", {
        get: function () {
            return this._channel;
        },
        set: function (value) {
            if (this._channel && this._channel.strFIOSServiceId != value.strFIOSServiceId) this.resetVars();else if (this._channel && this._channel.strFIOSServiceId == value.strFIOSServiceId && this._channel.dtCreateDate == value.dtCreateDate && this._channel.strStationCallSign == value.strStationCallSign && this._channel.strStationCallSign == value.strStationCallSign && this._channel.strStationDescription == value.strStationDescription) return;
            this._channel = value;
            if (this._activeRegions) this.loadRegions();
            this.loadStation();
        },
        enumerable: true,
        configurable: true
    });
    ;
    ChannelInfoComponent.prototype.ngOnInit = function () {
        this.getActiveRegions();
    };
    ChannelInfoComponent.prototype.getActiveRegions = function () {
        var _this = this;
        console.log('getActiveRegions called');
        this._channelService.get('/api/region/active').finally(function () {
            _this.loadRegions();
        }).subscribe(function (x) {
            _this._activeRegions = x;
        });
    };
    ChannelInfoComponent.prototype.loadRegions = function () {
        var _this = this;
        console.log('loadRegions called');
        if (!this._channel) {
            return;
        }
        this.regionLoading = true;
        this._channelService.getBy('/api/channel/', this._channel.strFIOSServiceId).map(function (arr) {
            return arr.filter(function (ch) {
                return _this._activeRegions.includes(ch.strFIOSRegionName);
            }).map(function (ch) {
                return { name: ch.strFIOSRegionName, channel: ch.intChannelPosition, genre: ch.strStationGenre };
            });
        }).finally(function () {
            _this.regionLoading = false;
            if (!_this.stationLoading) setTimeout(function () {
                _this.onshow.emit();
            });
        }).subscribe(function (x) {
            _this.regions = _.uniqBy(x, function (reg) {
                return [reg.strFIOSRegionName, reg.intChannelPosition, reg.strStationGenre].join();
            });
        });
    };
    ChannelInfoComponent.prototype.loadStation = function () {
        var _this = this;
        console.log('loadStation called');
        this.stationLoading = true;
        if (!this._channel) {
            return;
        }
        this._channelService.getBy('/api/station/', this._channel.strFIOSServiceId).finally(function () {
            _this.stationLoading = false;
            if (!_this.regionLoading) setTimeout(function () {
                _this.onshow.emit();
            });
        }).subscribe(function (x) {
            _this._station = x;
        });
    };
    ChannelInfoComponent.prototype.onEdit = function ($event) {
        console.log("onEdit event called");
        this.resetVars();
        var target = $event.target;
        var title = target.title;
        this.editField = title;
    };
    ChannelInfoComponent.prototype.onFieldChange = function ($event) {
        console.log("onFieldChange called");
        var newValue = $event.target.value;
        switch (this.editField) {
            case "station_name":
                {
                    if (this.channel.strStationName != newValue) {
                        this.showSubmit = true;
                        this._station.strStationName = newValue;
                    }
                    break;
                }
            case "station_desc":
                {
                    if (this.channel.strStationDescription != newValue) {
                        this.showSubmit = true;
                        this._station.strStationDescription = newValue;
                    }
                    break;
                }
            case "station_callsign":
                {
                    if (this.channel.strStationCallSign != newValue) {
                        this.showSubmit = true;
                        this._station.strStationCallSign = newValue;
                    }
                    break;
                }
        }
    };
    ChannelInfoComponent.prototype.onFieldFocusOut = function () {
        console.log("onFieldFocusOut called");
        this.editField = '';
    };
    ChannelInfoComponent.prototype.onSubmit = function () {
        var _this = this;
        console.log("onSubmit called");
        this.isSubmitting = true;
        if (!this._station) {
            this.errorMsg = 'Failed to load Fios Station.';
            this.showSubmit = false;
            return;
        }
        this._channelService.put('/api/station', this._station).finally(function () {
            _this.loadStation();
            _this.stationchange.emit(_this._channel.strFIOSServiceId);
        }).subscribe(function (resp) {
            _this.isSubmitting = false;
            _this.isSuccess = true;
        }, function (error) {
            _this.errorMsg = error;
            _this.isSuccess = false;
            _this.isSubmitting = false;
        });
        this.showSubmit = false;
    };
    ChannelInfoComponent.prototype.onCancel = function ($event) {
        console.log("onCancel called");
        this.resetVars();
        this.showSubmit = false;
        this._station.strStationName = this._channel.strStationName;
        this._station.strStationCallSign = this._channel.strStationCallSign;
        this._station.strStationDescription = this._channel.strStationDescription;
    };
    ChannelInfoComponent.prototype.resetVars = function () {
        this.editField = '';
        this.isSubmitting = false;
        this.errorMsg = undefined;
        this.isSuccess = false;
    };
    return ChannelInfoComponent;
}();
__decorate([core_1.Input(), __metadata("design:type", Object), __metadata("design:paramtypes", [Object])], ChannelInfoComponent.prototype, "channel", null);
__decorate([core_1.Output(), __metadata("design:type", core_1.EventEmitter)], ChannelInfoComponent.prototype, "stationchange", void 0);
__decorate([core_1.Output(), __metadata("design:type", core_1.EventEmitter)], ChannelInfoComponent.prototype, "onshow", void 0);
ChannelInfoComponent = __decorate([core_1.Component({
    selector: 'channel-info',
    templateUrl: 'app/Components/channelinfo.component.html',
    styleUrls: ['app/Styles/channelinfo.component.css']
}), __metadata("design:paramtypes", [channel_service_1.ChannelService])], ChannelInfoComponent);
exports.ChannelInfoComponent = ChannelInfoComponent;
var FocusableInput = function () {
    function FocusableInput() {
        this.onFieldChangeEvent = new core_1.EventEmitter();
        this.onFieldFocusOutEvent = new core_1.EventEmitter();
    }
    FocusableInput.prototype.ngAfterViewInit = function () {
        this.focusInput.nativeElement.focus();
    };
    FocusableInput.prototype.onFieldChange = function ($event) {
        this.onFieldChangeEvent.emit($event);
    };
    FocusableInput.prototype.onFieldFocusOut = function ($event) {
        this.onFieldFocusOutEvent.emit($event);
    };
    return FocusableInput;
}();
__decorate([core_1.ViewChild('focusinput'), __metadata("design:type", core_1.ElementRef)], FocusableInput.prototype, "focusInput", void 0);
__decorate([core_1.Input('model'), __metadata("design:type", Object)], FocusableInput.prototype, "model", void 0);
__decorate([core_1.Input('inputValue'), __metadata("design:type", String)], FocusableInput.prototype, "inputValue", void 0);
__decorate([core_1.Output('onFieldChange'), __metadata("design:type", core_1.EventEmitter)], FocusableInput.prototype, "onFieldChangeEvent", void 0);
__decorate([core_1.Output('onFieldFocusOut'), __metadata("design:type", core_1.EventEmitter)], FocusableInput.prototype, "onFieldFocusOutEvent", void 0);
FocusableInput = __decorate([core_1.Component({
    selector: 'focusable-input',
    template: "\n        <input #focusinput [(ngModel)]=\"model\" value=\"{{inputValue}}\" (input)=\"onFieldChange($event)\" (focusout)=\"onFieldFocusOut($event)\" autofocus/>\n    ",
    styles: ['width:100%']
})], FocusableInput);
exports.FocusableInput = FocusableInput;
//# sourceMappingURL=channelinfo.component.js.map