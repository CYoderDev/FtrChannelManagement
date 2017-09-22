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
const channellogo_service_1 = require("../Service/channellogo.service");
const forms_1 = require("@angular/forms");
const ng2_bs3_modal_1 = require("ng2-bs3-modal/ng2-bs3-modal");
require("rxjs/add/operator/map");
require("rxjs/add/operator/switchMap");
require("rxjs/add/operator/do");
require("rxjs/add/operator/catch");
require("rxjs/add/observable/of");
require("rxjs/add/observable/from");
require("rxjs/add/operator/toArray");
var editLogoAction;
(function (editLogoAction) {
    editLogoAction[editLogoAction["all"] = 0] = "all";
    editLogoAction[editLogoAction["single"] = 1] = "single";
})(editLogoAction || (editLogoAction = {}));
;
let EditLogoForm = class EditLogoForm {
    constructor(fb, _channelService, _channelLogoService, element) {
        this.fb = fb;
        this._channelService = _channelService;
        this._channelLogoService = _channelLogoService;
        this.element = element;
        this.showForm = false;
        this.showStations = true;
        this.action = editLogoAction.all;
    }
    set channel(value) {
        if (value && value != this.channel && this.action == editLogoAction.all && this.showForm) {
            console.log("set channel called");
            console.log('opening modal');
            this._channel = value;
            this.loadStations();
            this.modalChLogo.open();
        }
    }
    ;
    get channel() {
        return this._channel;
    }
    ngOnInit() {
        this.isLoading = true;
        if (null == this.channel) {
            this.showForm = false;
            return;
        }
        ;
        this.isLoading = false;
    }
    onActionChange($obj) {
        console.log("onActionChange called");
        var descElement = document.getElementById('action-desc');
        if ($obj.target.value == 'all') {
            this.action = editLogoAction.all;
            descElement.innerHTML = "Updates the logo for all stations to which this logo is currently assigned (listed below).";
            this.showStations = true;
        }
        else {
            this.action = editLogoAction.single;
            descElement.innerHTML = "Creates a new logo and assigns it only to this station.";
            this.showStations = false;
        }
    }
    loadStations() {
        this._channelLogoService.getBy('api/channellogo/{0}/station', this.channel.intBitMapId).subscribe(x => {
            this.stations = x;
        }, error => this.errorMsg = error);
    }
    getUri(bitmapId) {
        if (!bitmapId)
            return;
        return "/ChannelLogoRepository/" + bitmapId.toString() + ".png";
    }
    newImageChange($event) {
        console.log("newImageChange called.");
        var reader = new FileReader();
        var image = document.getElementById('newImage');
        reader.onload = function (e) {
            var src = reader.result;
            image.setAttribute('src', src);
        };
        reader.readAsDataURL($event.target.files[0]);
        this.newImage = image.getAttribute('src');
    }
};
__decorate([
    core_1.ViewChild('modalChLogo'),
    __metadata("design:type", ng2_bs3_modal_1.ModalComponent)
], EditLogoForm.prototype, "modalChLogo", void 0);
__decorate([
    core_1.Input(),
    __metadata("design:type", Object),
    __metadata("design:paramtypes", [Object])
], EditLogoForm.prototype, "channel", null);
EditLogoForm = __decorate([
    core_1.Component({
        selector: 'editLogoForm',
        templateUrl: 'app/Components/editlogo.component.html',
        styleUrls: ['app/Styles/editlogo.component.css']
    }),
    __metadata("design:paramtypes", [forms_1.FormBuilder, channel_service_1.ChannelService, channellogo_service_1.ChannelLogoService, core_1.ElementRef])
], EditLogoForm);
exports.EditLogoForm = EditLogoForm;
