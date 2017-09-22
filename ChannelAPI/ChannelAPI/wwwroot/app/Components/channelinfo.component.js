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
const channel_service_1 = require("../Service/channel.service");
let ChannelInfoComponent = class ChannelInfoComponent {
    constructor(_channelService) {
        this._channelService = _channelService;
    }
    set channel(value) {
        this._channel = value;
        this.loadRegions();
    }
    get channel() { return this._channel; }
    ;
    ngOnInit() {
    }
    loadRegions() {
        console.log('loadRegions called');
        if (!this._channel) {
            return;
        }
        let channels;
        this._channelService.getBy('/api/channel/', this._channel.strFIOSServiceId)
            .map(arr => arr.map((ch) => {
            return { name: ch.strFIOSRegionName, channel: ch.intChannelPosition, genre: ch.strStationGenre };
        }))
            .subscribe(x => {
            this.regions = x;
        });
    }
};
__decorate([
    core_1.Input(),
    __metadata("design:type", Object),
    __metadata("design:paramtypes", [Object])
], ChannelInfoComponent.prototype, "channel", null);
ChannelInfoComponent = __decorate([
    core_1.Component({
        selector: 'channel-info',
        templateUrl: 'app/Components/channelinfo.component.html',
        styleUrls: ['app/Styles/channelinfo.component.css']
    }),
    __metadata("design:paramtypes", [channel_service_1.ChannelService])
], ChannelInfoComponent);
exports.ChannelInfoComponent = ChannelInfoComponent;
//# sourceMappingURL=channelinfo.component.js.map