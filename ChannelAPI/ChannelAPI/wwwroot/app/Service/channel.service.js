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
require("rxjs/add/observable/from");
require("rxjs/add/observable/throw");
require("rxjs/add/observable/empty");
let ChannelService = class ChannelService {
    constructor(_http) {
        this._http = _http;
    }
    get(url) {
        return this._http.get(url)
            .map((response) => response.json())
            .catch(this.handleError);
    }
    getBy(url, id) {
        return this._http.get(url + id)
            .map((response) => response.json())
            .catch(this.handleError);
    }
    getBriefBy(id) {
        return this._http.get('api/channel/' + id)
            .map((response) => response.json())
            .map((x) => {
            return x.map(y => {
                return { id: y.strFIOSServiceId, num: y.intChannelPosition, name: y.strStationName, region: y.strFIOSRegionName, call: y.strStationCallSign, logoid: y.intBitMapId };
            }).pop();
        }).catch(this.handleError);
    }
    put(url, obj) {
        let body = JSON.stringify(obj);
        let headers = new http_1.Headers({ 'Content-Type': 'application/json' });
        let options = new http_1.RequestOptions({ headers: headers });
        return this._http.put(url, body, options)
            .map((response) => {
            if (response.ok)
                return Observable_1.Observable.empty();
            else
                throw new Error('Http request has failed. Status: ' + response.status + ' - ' + response.statusText);
        })
            .catch(this.handleError);
    }
    handleError(error) {
        console.error(error);
        return Observable_1.Observable.throw(error.json().error || 'Server error');
    }
};
ChannelService = __decorate([
    core_1.Injectable(),
    __metadata("design:paramtypes", [http_1.Http])
], ChannelService);
exports.ChannelService = ChannelService;
//# sourceMappingURL=channel.service.js.map