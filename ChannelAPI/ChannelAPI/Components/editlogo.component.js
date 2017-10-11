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
const ng2_bs3_modal_1 = require("ng2-bs3-modal");
const Observable_1 = require("rxjs/Observable");
require("rxjs/add/operator/map");
require("rxjs/add/operator/switchMap");
require("rxjs/add/operator/do");
require("rxjs/add/operator/catch");
require("rxjs/add/observable/of");
var editLogoAction;
(function (editLogoAction) {
    editLogoAction[editLogoAction["all"] = 0] = "all";
    editLogoAction[editLogoAction["single"] = 1] = "single";
})(editLogoAction || (editLogoAction = {}));
;
let EditLogoForm = class EditLogoForm {
    constructor(_channelLogoService) {
        this._channelLogoService = _channelLogoService;
        this.channelchange = new core_1.EventEmitter();
        this.showForm = false;
        this.showStations = true;
        this.stationsLoading = false;
        this.isSuccess = false;
        this.submitting = false;
        this.action = editLogoAction.all;
    }
    set channel(value) {
        if ((!this.channel && value) || value && (value.strFIOSServiceId != this.channel.strFIOSServiceId
            || this.channel.dtCreateDate != value.dtCreateDate)) {
            console.log("set channel called");
            this._channel = value;
            this.imgSource = this.getUri(value.intBitMapId);
            if (!this.stations && this.action == editLogoAction.all && this.showForm)
                this.loadStations();
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
        this.isSuccess = false;
        var descElement = document.getElementById('action-desc');
        if ($obj.target.value == 'all') {
            this.loadStations();
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
    OpenForm() {
        console.log('opening edit logo modal');
        if (this._channel)
            this.loadStations();
        this.modalChLogo.open();
    }
    loadStations() {
        console.log('Loading stations from edit logo modal');
        this.stationsLoading = true;
        this._channelLogoService.getBy('api/channellogo/{0}/station', this.channel.intBitMapId)
            .subscribe(x => {
            this.stations = x;
        }, error => this.errorMsg = error, () => { this.stationsLoading = false; });
    }
    getUri(bitmapId) {
        if (!bitmapId)
            return;
        return "/ChannelLogoRepository/" + bitmapId.toString() + ".png";
    }
    newImageChange($event) {
        console.log("newImageChange called.");
        if (!$event || !$event.target.files || $event.target.files.length < 1)
            return;
        this.isSuccess = false;
        var reader = new FileReader();
        var image = document.getElementById('newImage');
        reader.onload = function (e) {
            var src = reader.result;
            image.setAttribute('src', src);
        };
        reader.readAsDataURL($event.target.files[0]);
        this.newImage = $event.target.files[0];
    }
    onSubmit() {
        var nextId;
        var duplicate;
        this.getDuplicates()
            .switchMap((value) => {
            if (!value) {
                duplicate = false;
                return this.getNextId();
            }
            else {
                duplicate = true;
                return Observable_1.Observable.of(value);
            }
        })
            .subscribe((observer) => {
            nextId = parseInt(observer);
            if (isNaN(nextId))
                throw new Error("Failed to get image identifier.");
        }, (error) => {
            console.log(error);
            this.errorMsg = error;
        }, () => {
            if (this.errorMsg)
                return;
            if (this.action == editLogoAction.all && duplicate) {
                this.updateStation(this.stations, nextId);
            }
            else if (this.action == editLogoAction.all && !duplicate) {
                this.updateLogo(this.channel.intBitMapId);
            }
            else if (duplicate) {
                this.updateStation([this.channel], nextId);
            }
            else {
                this.createLogo(this.channel.strFIOSServiceId, nextId);
            }
        });
    }
    getDuplicates() {
        console.log('getDuplicates called');
        this.submitting = true;
        return this._channelLogoService.performRequest('/api/channellogo/image/duplicate', 'PUT', this.newImage, 'application/octet-stream', this.newImage.type);
    }
    getNextId() {
        console.log('getNextId called');
        return this._channelLogoService.get('/api/channellogo/nextid');
    }
    updateStation(stations, bitmapId, index = 0) {
        if (index >= stations.length)
            return;
        this._channelLogoService.performRequest('/api/channellogo/' + bitmapId + '/station/' + stations[index].strFIOSServiceId, 'PUT', null, 'application/json')
            .subscribe((observer) => {
            console.log('%i stations updated with id %s', observer, stations[index].strFIOSServiceId);
        }, (error) => {
            console.log('Failed to update station id %s. %s', stations[index].strFIOSServiceId, error);
            this.errorMsg = 'Failed to update station - ' + error;
        }, () => {
            this.inputImg.nativeElement.value = "";
            if (index + 1 == stations.length) {
                this.loadStations();
                if (!this.errorMsg) {
                    this.isSuccess = true;
                }
                this.submitting = false;
            }
            this.channelchange.emit(stations[index].strFIOSServiceId);
            this.updateStation(stations, bitmapId, ++index);
        });
    }
    updateLogo(bitmapId) {
        console.log('updateLogo called', bitmapId);
        return this._channelLogoService.performRequest('api/channellogo/image/' + bitmapId.toString(), 'PUT', this.newImage, 'application/octet-stream', this.newImage.type)
            .subscribe((val) => {
            this.channel.intBitMapId = bitmapId;
        }, (error) => {
            console.log(error);
            this.errorMsg = "Failed to update logo. " + error;
        }, () => {
            this.inputImg.nativeElement.value = "";
            this.submitting = false;
            if (!this.errorMsg) {
                this.isSuccess = true;
                this.stations.forEach((station) => {
                    this.channelchange.emit(station.strFIOSServiceId);
                });
            }
        });
    }
    createLogo(fiosid, bitmapId) {
        console.log('createLogo called', fiosid, bitmapId);
        return this._channelLogoService.performRequest('/api/station/' + fiosid + '/logo/' + bitmapId.toString(), 'PUT', this.newImage, 'application/octet-stream', this.newImage.type)
            .do((val) => { console.log('Stations affected: ' + val); })
            .subscribe((val) => {
            this.channel.intBitMapId = bitmapId;
        }, (error) => {
            console.log(error);
            this.errorMsg = "Failed to create new logo. " + error;
        }, () => {
            this.inputImg.nativeElement.value = "";
            this.submitting = false;
            if (!this.errorMsg) {
                this.isSuccess = true;
                this.channelchange.emit(fiosid);
            }
        });
    }
    onModalClose() {
        console.log('modal closing');
        this.showForm = false;
        this._channel = undefined;
        this.isSuccess = false;
        this.stations = undefined;
    }
};
__decorate([
    core_1.ViewChild('modalChLogo'),
    __metadata("design:type", ng2_bs3_modal_1.BsModalComponent)
], EditLogoForm.prototype, "modalChLogo", void 0);
__decorate([
    core_1.ViewChild('inputImg'),
    __metadata("design:type", core_1.ElementRef)
], EditLogoForm.prototype, "inputImg", void 0);
__decorate([
    core_1.Input(),
    __metadata("design:type", Object),
    __metadata("design:paramtypes", [Object])
], EditLogoForm.prototype, "channel", null);
__decorate([
    core_1.Output(),
    __metadata("design:type", core_1.EventEmitter)
], EditLogoForm.prototype, "channelchange", void 0);
EditLogoForm = __decorate([
    core_1.Component({
        selector: 'editLogoForm',
        templateUrl: 'app/Components/editlogo.component.html',
        styleUrls: ['app/Styles/editlogo.component.css']
    }),
    __metadata("design:paramtypes", [channellogo_service_1.ChannelLogoService])
], EditLogoForm);
exports.EditLogoForm = EditLogoForm;
