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
var ChannelLogoService = (function () {
    function ChannelLogoService(_http) {
        this._http = _http;
        this.openLocalImage = function (file, callback) {
            var fileReader = new FileReader();
            fileReader.onloadend = function (e) {
                return callback(fileReader.result);
            };
            fileReader.readAsDataURL(file);
        };
    }
    ChannelLogoService.prototype.get = function (url) {
        return this._http.get(url, new http_1.RequestOptions({ withCredentials: true }))
            .map(function (response) { return response.json(); })
            .catch(this.handleError);
    };
    ChannelLogoService.prototype.getBy = function (url, id) {
        url = url.replace('{0}', id.toString());
        return this._http.get(url, new http_1.RequestOptions({ withCredentials: true }))
            .map(function (response) { return response.json(); })
            .catch(this.handleError);
    };
    ChannelLogoService.prototype.getByBody = function (url, body) {
        var _this = this;
        var ret = this.openLocalImage(body, function (val) {
            return _this._http.get(url, new http_1.RequestOptions({
                body: val,
                headers: new http_1.Headers({ 'Content-Type': 'image/png' }),
                withCredentials: true
            }))
                .map(function (response) { return response.json(); })
                .catch(_this.handleError);
        });
        return ret.readAsDataURL(body);
    };
    ChannelLogoService.prototype.putBody = function (url, obj) {
        var _this = this;
        var headers = new http_1.Headers({ 'Content-Type': 'image/png' });
        var ret = this.openLocalImage(obj, function (val) {
            _this._http.put(url, val, new http_1.RequestOptions({ headers: headers, withCredentials: true }))
                .map(function (response) {
                if (response.ok)
                    return Observable_1.Observable.empty;
                else
                    throw new Error('Http PUT request failed. Status: ' + response.status + ' - ' + response.statusText);
            })
                .catch(_this.handleError);
        });
        return ret.readAsDataURL(obj);
    };
    ChannelLogoService.prototype.put = function (url) {
        return this._http.put(url, null, new http_1.RequestOptions({ withCredentials: true }))
            .map(function (response) {
            if (response.ok)
                return Observable_1.Observable.empty;
            else
                throw new Error('Http PUT request failed. Status: ' + response.status + ' - ' + response.statusText);
        })
            .catch(this.handleError);
    };
    ChannelLogoService.prototype.post = function (url, obj) {
        var headers = new http_1.Headers({ 'Content-Type': 'image/png' });
        return this._http.post(url, obj, new http_1.RequestOptions({ headers: headers, withCredentials: true }))
            .map(function (response) {
            if (response.ok)
                return Observable_1.Observable.empty;
            else
                throw new Error('Http POST request failed. Status: ' + response.status + ' - ' + response.statusText);
        });
    };
    ChannelLogoService.prototype.performRequest = function (endPoint, method, body, contentType, uploadContentType) {
        if (body === void 0) { body = null; }
        if (uploadContentType === void 0) { uploadContentType = null; }
        var headers = new http_1.Headers({ 'Content-Type': contentType });
        var options = new http_1.RequestOptions({ headers: headers, withCredentials: true });
        if (body)
            options.body = body;
        if (uploadContentType)
            headers.append('Upload-Content-Type', uploadContentType);
        if (method == 'GET') {
            headers.append('Cache-Control', 'no-cache,no-store');
            return this._http.get(endPoint, options)
                .map(this.extractData)
                .catch(this.handleError);
        }
        else if (method == 'PUT') {
            options.withCredentials = true;
            return this._http.put(endPoint, body, options)
                .map(this.extractData)
                .catch(this.handleError);
        }
        else {
            options.withCredentials = true;
            return this._http.post(endPoint, body, options)
                .map(this.extractData)
                .catch(this.handleError);
        }
    };
    ChannelLogoService.prototype.convertToBase64 = function (inputValue) {
        var file = inputValue;
        var reader = new FileReader();
        var image;
        reader.onloadend = function (e) {
            e.target["result"];
            return reader.result;
        };
        reader.readAsDataURL(file);
    };
    ChannelLogoService.prototype.extractData = function (response) {
        var contentType = response.headers.get('Content-Type');
        if (contentType) {
            if (contentType.startsWith('image'))
                return response.text();
        }
        return response.json();
    };
    ChannelLogoService.prototype.handleError = function (error) {
        console.error(error);
        if (error instanceof http_1.Response)
            return Observable_1.Observable.throw(error.statusText || 'Backend Server error');
        else
            return Observable_1.Observable.throw(error || 'Backend Server Error');
    };
    ChannelLogoService = __decorate([
        core_1.Injectable(),
        __metadata("design:paramtypes", [http_1.Http])
    ], ChannelLogoService);
    return ChannelLogoService;
}());
exports.ChannelLogoService = ChannelLogoService;
