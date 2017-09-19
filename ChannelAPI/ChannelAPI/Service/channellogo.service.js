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
const http_1 = require("@angular/http");
const Observable_1 = require("rxjs/Observable");
require("rxjs/add/operator/map");
require("rxjs/add/operator/do");
require("rxjs/add/operator/catch");
let ChannelLogoService = class ChannelLogoService {
    constructor(_http) {
        this._http = _http;
    }
    get(url) {
        return this._http.get(url)
            .map((response) => response.json())
            .catch(this.handleError);
    }
    getBy(url, id) {
        url = url.replace('{0}', id.toString());
        return this._http.get(url)
            .map((response) => response.json())
            .catch(this.handleError);
    }
    getByBody(url, body) {
        return this._http.get(url, new http_1.RequestOptions({
            body: body
        }))
            .map((response) => response.json())
            .catch(this.handleError);
    }
    convertToBase64(inputValue) {
        var file = inputValue.files[0];
        var reader = new FileReader();
        var image;
        reader.onloadend = (e) => {
            image = reader.result;
        };
        reader.readAsDataURL(file);
        return image;
    }
    handleError(error) {
        console.error(error);
        return Observable_1.Observable.throw(error.json().error || 'Server error');
    }
};
ChannelLogoService = __decorate([
    core_1.Injectable(),
    __metadata("design:paramtypes", [http_1.Http])
], ChannelLogoService);
exports.ChannelLogoService = ChannelLogoService;
