"use strict";
var __decorate = (this && this.__decorate) || function (decorators, target, key, desc) {
    var c = arguments.length, r = c < 3 ? target : desc === null ? desc = Object.getOwnPropertyDescriptor(target, key) : desc, d;
    if (typeof Reflect === "object" && typeof Reflect.decorate === "function") r = Reflect.decorate(decorators, target, key, desc);
    else for (var i = decorators.length - 1; i >= 0; i--) if (d = decorators[i]) r = (c < 3 ? d(r) : c > 3 ? d(target, key, r) : d(target, key)) || r;
    return c > 3 && r && Object.defineProperty(target, key, r), r;
};
var __metadata = (this && this.__metadata) || function (k, v) {
    if (typeof Reflect === "object" && typeof Reflect.metadata === "function") return Reflect.metadata(k, v);
};
Object.defineProperty(exports, "__esModule", { value: true });
const core_1 = require("@angular/core");
const _ = require("lodash");
require("rxjs/add/operator/filter");
require("rxjs/add/operator/finally");
const channel_service_1 = require("../Service/channel.service");
let ChannelInfoComponent = class ChannelInfoComponent {
    constructor(_channelService) {
        this._channelService = _channelService;
        this.stationchange = new core_1.EventEmitter();
        this.onshow = new core_1.EventEmitter();
        this.showSubmit = false;
        this.isSubmitting = false;
        this.stationLoading = false;
        this.regionLoading = false;
        this.editField = '';
    }
    set channel(value) {
        if (this._channel && this._channel.strFIOSServiceId != value.strFIOSServiceId)
            this.resetVars();
        else if (this._channel && this._channel.strFIOSServiceId == value.strFIOSServiceId
            && this._channel.dtCreateDate == value.dtCreateDate && this._channel.strStationCallSign == value.strStationCallSign
            && this._channel.strStationCallSign == value.strStationCallSign && this._channel.strStationDescription == value.strStationDescription)
            return;
        this._channel = value;
        if (this._activeRegions)
            this.loadRegions();
        this.loadStation();
    }
    get channel() { return this._channel; }
    ;
    ngOnInit() {
        this.getActiveRegions();
    }
    getActiveRegions() {
        console.log('getActiveRegions called');
        this._channelService.get('/api/region/active')
            .finally(() => {
            this.loadRegions();
        })
            .subscribe(x => {
            this._activeRegions = x;
        });
    }
    loadRegions() {
        console.log('loadRegions called');
        if (!this._channel) {
            return;
        }
        this.regionLoading = true;
        this._channelService.getBy('/api/channel/', this._channel.strFIOSServiceId)
            .map(arr => arr
            .filter(ch => {
            return this._activeRegions.includes(ch.strFIOSRegionName);
        })
            .map((ch) => {
            return { name: ch.strFIOSRegionName, channel: ch.intChannelPosition, genre: ch.strStationGenre };
        }))
            .finally(() => {
            this.regionLoading = false;
            if (!this.stationLoading)
                setTimeout(() => { this.onshow.emit(); });
        })
            .subscribe(x => {
            this.regions = _.uniqBy(x, function (reg) { return [reg.strFIOSRegionName, reg.intChannelPosition, reg.strStationGenre].join(); });
        });
    }
    loadStation() {
        console.log('loadStation called');
        this.stationLoading = true;
        if (!this._channel) {
            return;
        }
        this._channelService.getBy('/api/station/', this._channel.strFIOSServiceId)
            .finally(() => {
            this.stationLoading = false;
            if (!this.regionLoading)
                setTimeout(() => { this.onshow.emit(); });
        })
            .subscribe(x => {
            this._station = x;
        });
    }
    onEdit($event) {
        console.log("onEdit event called");
        this.resetVars();
        var target = $event.target;
        var title = target.title;
        this.editField = title;
    }
    onFieldChange($event) {
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
    }
    onFieldFocusOut() {
        console.log("onFieldFocusOut called");
        this.editField = '';
    }
    onSubmit() {
        console.log("onSubmit called");
        this.isSubmitting = true;
        if (!this._station) {
            this.errorMsg = 'Failed to load Fios Station.';
            this.showSubmit = false;
            return;
        }
        this._channelService.put('/api/station', this._station)
            .finally(() => {
            this.loadStation();
            this.stationchange.emit(this._channel.strFIOSServiceId);
        })
            .subscribe((resp) => {
            this.isSubmitting = false;
            this.isSuccess = true;
        }, (error) => {
            this.errorMsg = error;
            this.isSuccess = false;
            this.isSubmitting = false;
        });
        this.showSubmit = false;
    }
    onCancel($event) {
        console.log("onCancel called");
        this.resetVars();
        this.showSubmit = false;
        this._station.strStationName = this._channel.strStationName;
        this._station.strStationCallSign = this._channel.strStationCallSign;
        this._station.strStationDescription = this._channel.strStationDescription;
    }
    resetVars() {
        this.editField = '';
        this.isSubmitting = false;
        this.errorMsg = undefined;
        this.isSuccess = false;
    }
};
__decorate([
    core_1.Input(),
    __metadata("design:type", Object),
    __metadata("design:paramtypes", [Object])
], ChannelInfoComponent.prototype, "channel", null);
__decorate([
    core_1.Output(),
    __metadata("design:type", core_1.EventEmitter)
], ChannelInfoComponent.prototype, "stationchange", void 0);
__decorate([
    core_1.Output(),
    __metadata("design:type", core_1.EventEmitter)
], ChannelInfoComponent.prototype, "onshow", void 0);
ChannelInfoComponent = __decorate([
    core_1.Component({
        selector: 'channel-info',
        templateUrl: 'app/Components/channelinfo.component.html',
        styleUrls: ['app/Styles/channelinfo.component.css']
    }),
    __metadata("design:paramtypes", [channel_service_1.ChannelService])
], ChannelInfoComponent);
exports.ChannelInfoComponent = ChannelInfoComponent;
let FocusableInput = class FocusableInput {
    constructor() {
        this.onFieldChangeEvent = new core_1.EventEmitter();
        this.onFieldFocusOutEvent = new core_1.EventEmitter();
    }
    ngAfterViewInit() {
        this.focusInput.nativeElement.focus();
    }
    onFieldChange($event) {
        this.onFieldChangeEvent.emit($event);
    }
    onFieldFocusOut($event) {
        this.onFieldFocusOutEvent.emit($event);
    }
};
__decorate([
    core_1.ViewChild('focusinput'),
    __metadata("design:type", core_1.ElementRef)
], FocusableInput.prototype, "focusInput", void 0);
__decorate([
    core_1.Input('model'),
    __metadata("design:type", Object)
], FocusableInput.prototype, "model", void 0);
__decorate([
    core_1.Input('inputValue'),
    __metadata("design:type", String)
], FocusableInput.prototype, "inputValue", void 0);
__decorate([
    core_1.Output('onFieldChange'),
    __metadata("design:type", core_1.EventEmitter)
], FocusableInput.prototype, "onFieldChangeEvent", void 0);
__decorate([
    core_1.Output('onFieldFocusOut'),
    __metadata("design:type", core_1.EventEmitter)
], FocusableInput.prototype, "onFieldFocusOutEvent", void 0);
FocusableInput = __decorate([
    core_1.Component({
        selector: 'focusable-input',
        template: `
        <input #focusinput [(ngModel)]="model" value="{{inputValue}}" (input)="onFieldChange($event)" (focusout)="onFieldFocusOut($event)" autofocus/>
    `,
        styles: ['width:100%']
    })
], FocusableInput);
exports.FocusableInput = FocusableInput;
//# sourceMappingURL=channelinfo.component.js.map