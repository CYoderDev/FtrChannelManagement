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
const channellogo_service_1 = require("../Service/channellogo.service");
let ChannelLogoComponent = class ChannelLogoComponent {
    constructor(_channelLogoService) {
        this._channelLogoService = _channelLogoService;
        this.isLoading = false;
        this.bitmapId = "10000";
        console.log("ChannelLogoConstructor called");
    }
    ngOnInit() {
    }
};
__decorate([
    core_1.Input(),
    __metadata("design:type", String)
], ChannelLogoComponent.prototype, "bitmapId", void 0);
ChannelLogoComponent = __decorate([
    core_1.Component({
        selector: "channellogo",
        template: `
        <img [src]="image" alt="No Logo Found" *ngIf="!isLoading" />
        <div *ngIf="isLoading">Loading...</div>
`
    }),
    __metadata("design:paramtypes", [channellogo_service_1.ChannelLogoService])
], ChannelLogoComponent);
exports.ChannelLogoComponent = ChannelLogoComponent;
