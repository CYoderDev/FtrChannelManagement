"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
var Logger = (function () {
    function Logger() {
    }
    Logger.prototype.error = function (args) {
        var optionalArgs = [];
        for (var _i = 1; _i < arguments.length; _i++) {
            optionalArgs[_i - 1] = arguments[_i];
        }
    };
    Logger.prototype.info = function (args) {
        var optionalArgs = [];
        for (var _i = 1; _i < arguments.length; _i++) {
            optionalArgs[_i - 1] = arguments[_i];
        }
    };
    Logger.prototype.log = function (args) {
        var optionalArgs = [];
        for (var _i = 1; _i < arguments.length; _i++) {
            optionalArgs[_i - 1] = arguments[_i];
        }
    };
    Logger.prototype.warn = function (args) {
        var optionalArgs = [];
        for (var _i = 1; _i < arguments.length; _i++) {
            optionalArgs[_i - 1] = arguments[_i];
        }
    };
    return Logger;
}());
exports.Logger = Logger;
