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
var core_1 = require("@angular/core");
var channellogo_service_1 = require("../Service/channellogo.service");
var ImageCellRendererComponent = (function () {
    function ImageCellRendererComponent(_channelLogoService) {
        this._channelLogoService = _channelLogoService;
        this.sourceBase = 'ChannelLogoRepository/';
    }
    ImageCellRendererComponent_1 = ImageCellRendererComponent;
    ImageCellRendererComponent.prototype.agInit = function (params) {
        this.params = params;
        this.source = this.sourceBase + this.params.value + '.png';
    };
    ImageCellRendererComponent.prototype.refresh = function (params) {
        var _this = this;
        this.source = undefined;
        this.params = params;
        this.logoEle.nativeElement.src = this.sourceBase + params.value + '.png?' + new Date().getTime();
        this.source = this.sourceBase + params.value + '.png?' + new Date().getTime();
        ImageCellRendererComponent_1.getLogo(this.params.value, this._channelLogoService)
            .subscribe(function (img) {
            _this.image = img;
        });
        return true;
    };
    ImageCellRendererComponent.getLogo = function (id, logoService) {
        return logoService.performRequest('ChannelLogoRepository/' + id + '.png', 'GET', null, 'application/json');
    };
    __decorate([
        core_1.ViewChild('logo'),
        __metadata("design:type", core_1.ElementRef)
    ], ImageCellRendererComponent.prototype, "logoEle", void 0);
    ImageCellRendererComponent = ImageCellRendererComponent_1 = __decorate([
        core_1.Component({
            selector: 'logo-cell',
            template: "\n            <div class=\"bg-black3d\" style=\"height: 100%; display: flex; flex-direction: column;\">\n                <div *ngIf=\"!source\" class=\"fa fa-2x fa-spinner fa-pulse\" aria-hidden=\"true\"></div>\n                <img *ngIf=\"source\" #logo src=\"{{source}}\" alt=\"No Logo\" style=\"flex-grow: 1; align-self: center;\" />\n                <div class='editBtnWrapper'>\n                    <button type=\"button\" class=\"btn btn-primary btn-xs\" title=\"Edit\" data-action-type=\"editlogo\">Edit</button>\n                </div>\n            </div>\n"
        }),
        __metadata("design:paramtypes", [channellogo_service_1.ChannelLogoService])
    ], ImageCellRendererComponent);
    return ImageCellRendererComponent;
    var ImageCellRendererComponent_1;
}());
exports.ImageCellRendererComponent = ImageCellRendererComponent;
