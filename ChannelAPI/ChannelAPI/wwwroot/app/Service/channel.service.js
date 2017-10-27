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
var http_1 = require("@angular/http");
var Observable_1 = require("rxjs/Observable");
require("rxjs/add/operator/map");
require("rxjs/add/operator/do");
require("rxjs/add/operator/catch");
require("rxjs/add/observable/from");
require("rxjs/add/observable/throw");
require("rxjs/add/observable/empty");
var ChannelService = (function () {
    function ChannelService(_http) {
        this._http = _http;
    }
    ChannelService.prototype.get = function (url) {
        var headers = new http_1.Headers({ 'If-Modified-Since': '0' });
        var options = new http_1.RequestOptions({ headers: headers, withCredentials: true });
        return this._http.get(url, options)
            .map(function (response) { return response.json(); })
            .catch(this.handleError);
    };
    ChannelService.prototype.getBy = function (url, id) {
        var headers = new http_1.Headers({ 'If-Modified-Since': '0' });
        var options = new http_1.RequestOptions({ headers: headers, withCredentials: true });
        return this._http.get(url + id, options)
            .map(function (response) { return response.json(); })
            .catch(this.handleError);
    };
    ChannelService.prototype.getBriefBy = function (id) {
        var headers = new http_1.Headers({ 'If-Modified-Since': '0' });
        var options = new http_1.RequestOptions({ headers: headers, withCredentials: true });
        return this._http.get('api/channel/' + id, options)
            .map(function (response) { return response.json(); })
            .map(function (x) {
            return x.map(function (y) {
                return { id: y.strFIOSServiceId, num: y.intChannelPosition, name: y.strStationName, region: y.strFIOSRegionName, call: y.strStationCallSign, logoid: y.intBitMapId };
            }).pop();
        }).catch(this.handleError);
    };
    ChannelService.prototype.put = function (url, obj) {
        var body = JSON.stringify(obj);
        var headers = new http_1.Headers({ 'Content-Type': 'application/json' });
        var options = new http_1.RequestOptions({ headers: headers, withCredentials: true });
        return this._http.put(url, body, options)
            .map(function (response) {
            if (response.ok)
                return Observable_1.Observable.empty();
            else
                throw new Error('Http request has failed. Status: ' + response.status + ' - ' + response.statusText);
        })
            .catch(this.handleError);
    };
    ChannelService.prototype.handleError = function (error) {
        console.error(error);
        if (error instanceof http_1.Response)
            return Observable_1.Observable.throw(error.json().error || 'Backend Server Error');
        else
            return Observable_1.Observable.throw(error || 'Backend Server Error');
    };
    return ChannelService;
}());
ChannelService = __decorate([
    core_1.Injectable(),
    __metadata("design:paramtypes", [http_1.Http])
], ChannelService);
exports.ChannelService = ChannelService;
