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
var channel_service_1 = require("../Service/channel.service");
var channellogo_service_1 = require("../Service/channellogo.service");
var editlogo_component_1 = require("./editlogo.component");
require("rxjs/add/operator/map");
require("rxjs/add/operator/do");
var _ = require("lodash");
var header_component_1 = require("./header.component");
var imagecellrender_component_1 = require("./imagecellrender.component");
var default_logger_service_1 = require("../Logging/default-logger.service");
var ChannelComponent = (function () {
    function ChannelComponent(_channelService, _channelLogoService, logger) {
        this._channelService = _channelService;
        this._channelLogoService = _channelLogoService;
        this.logger = logger;
        this.indLoading = false;
        this.updateChannel = false;
        this.showChannelInfo = true;
        logger.log("ChannelComponent constructor called");
        this.gridOptions = {
            getRowNodeId: function (data) { return data.id + data.region + data.num; }
        };
        this.showGrid = true;
        this.gridOptions.defaultColDef = {
            headerComponentFramework: header_component_1.HeaderComponent,
            headerComponentParams: {
                menuIcon: 'fa-bars'
            }
        };
    }
    ChannelComponent.prototype.ngOnInit = function () {
        this.logger.log("channelcomponent: ngOnInit() called");
        this.loadVhos();
    };
    ChannelComponent.prototype.createRowData = function () {
        var _this = this;
        this.logger.log("createRowData() called");
        this._channelService.getBy('api/channel/vho/', this.vho).map(function (arr) { return arr.map(function (ch) {
            return { id: ch.strFIOSServiceId, call: ch.strStationCallSign, name: ch.strStationName, num: ch.intChannelPosition, region: ch.strFIOSRegionName, logoid: ch.intBitMapId };
        }); })
            .subscribe(function (chb) {
            _this.gridOptions.api.setRowData(_.uniqBy(chb, function (ch) { return [ch.num, ch.region].join(); }));
            _this.gridOptions.api.hideOverlay();
        }, function (error) { return _this.msg = error; });
    };
    ChannelComponent.prototype.createColumnDefs = function () {
        this.logger.log("createColumnDefs called");
        this.columnDefs = [
            {
                headerName: 'Service ID',
                field: 'id',
                minWidth: 115,
                unSortIcon: false,
                cellClass: function (params) { return (params.value = 'center-cell'); },
                cellRenderer: defaultCellRenderer,
            },
            {
                headerName: 'Channel #',
                field: 'num',
                minWidth: 112,
                sort: 'asc',
                unSortIcon: false,
                cellClass: function (params) { return (params.value = 'center-cell'); },
                cellRenderer: defaultCellRenderer
            },
            {
                headerName: 'Call Sign',
                field: 'call',
                minWidth: 105,
                unSortIcon: false,
                cellClass: function (params) { return (params.value = 'center-cell'); },
                cellRenderer: defaultCellRenderer
            },
            {
                headerName: 'Station Name',
                field: 'name',
                unSortIcon: false,
                minWidth: 135,
                cellClass: function (params) { return (params.value = 'center-cell'); },
                cellRenderer: defaultCellRenderer
            },
            {
                headerName: 'Region',
                field: 'region',
                minWidth: 95,
                unSortIcon: false,
                cellClass: function (params) { return (params.value = 'center-cell'); },
                cellRenderer: defaultCellRenderer
            },
            {
                headerName: 'Logo',
                cellRendererFramework: imagecellrender_component_1.ImageCellRendererComponent,
                field: 'logoid',
                minWidth: 90,
                colId: 'logo',
                suppressFilter: true,
                suppressSorting: true,
            }
        ];
    };
    ChannelComponent.prototype.onReady = function () {
        this.logger.log('onReady');
        this.gridOptions.api.showLoadingOverlay();
        this.createColumnDefs();
        this.gridOptions.columnApi.autoSizeAllColumns();
        this.gridOptions.onGridSizeChanged = this.onGridSizeChanged;
        this.flexWidth(window.innerWidth);
        this.fitGridHeight(window.innerHeight);
    };
    ChannelComponent.prototype.onFilterModified = function () {
        this.logger.log("onFilterModified");
    };
    ChannelComponent.prototype.onRowClicked = function ($event) {
        this.logger.log("onRowClicked: " + $event.node.data.name);
        $event.node.setSelected(true, true);
        if ($event.event.target !== undefined) {
            var data = $event.data;
            var actionType = $event.event.target.getAttribute("data-action-type");
            switch (actionType) {
                case "editlogo":
                    this.editLogoForm.OpenForm();
                    this.editLogoForm.showForm = true;
                    break;
                default:
                    this.editLogoForm.showForm = false;
            }
        }
        this.loadChannel($event.node.data.id, $event.node.data.region);
    };
    ChannelComponent.prototype.toggleChannelInfo = function ($event) {
        this.showChannelInfo = !this.showChannelInfo;
    };
    ChannelComponent.prototype.onQuickFilterChanged = function ($event) {
        this.gridOptions.api.setQuickFilter($event.target.value);
    };
    ChannelComponent.prototype.onModelUpdated = function () {
        this.logger.log("onModelUpdated");
        if (this.gridOptions.api && this.columnDefs) {
            this.gridOptions.api.sizeColumnsToFit();
        }
    };
    ChannelComponent.prototype.onGridSizeChanged = function ($event) {
        if ($event && $event.api) {
            $event.api.sizeColumnsToFit();
        }
    };
    ChannelComponent.prototype.onVhoSelect = function ($event) {
        this.gridOptions.api.showLoadingOverlay();
        if (this.channel)
            this.channel = undefined;
        this.vhoSelect($event.target.value);
    };
    ChannelComponent.prototype.vhoSelect = function (vhoName) {
        var vho = vhoName.toLowerCase().startsWith('vho') ? vhoName.toLowerCase().replace('vho', '') : vhoName;
        if (this.vho != vho)
            if (this.vho != vho) {
                this.vho = vho;
                this.createRowData();
                this.gridOptions.api.redrawRows();
                this.fitGridHeight();
            }
    };
    ChannelComponent.prototype.columnResize = function () {
        this.logger.log("columnResize");
        if (this.gridOptions.api && this.columnDefs) {
            this.gridOptions.api.sizeColumnsToFit();
        }
    };
    ChannelComponent.prototype.updateRow = function ($event) {
        this.logger.log("updateRow called", $event);
        this.updateChannel = true;
        this.loadChannel($event, this.channel.strFIOSRegionName);
    };
    ChannelComponent.prototype.onWindowResize = function (event) {
        this.logger.log("onWindowsResize");
        var windowHeight = event.target.innerHeight;
        this.flexWidth(window.innerWidth);
        this.fitGridHeight(windowHeight);
    };
    ChannelComponent.prototype.flexWidth = function (width) {
        this.logger.log("flexWidth", width);
        var rowHeight = this.gridOptions.rowHeight;
        if (width <= 500) {
            this.gridOptions.rowHeight = 70;
            this.gridOptions.floatingFilter = false;
        }
        else if (width <= 768) {
            this.gridOptions.rowHeight = 78;
            this.gridOptions.floatingFilter = false;
        }
        else if (width <= 992) {
            this.gridOptions.rowHeight = 100;
            this.gridOptions.floatingFilter = true;
        }
        else {
            this.gridOptions.rowHeight = 110;
            this.gridOptions.floatingFilter = true;
        }
        if (rowHeight != this.gridOptions.rowHeight)
            this.gridOptions.api.resetRowHeights();
    };
    ChannelComponent.prototype.fitGridHeight = function (height) {
        this.logger.log("fitGridHeight", height);
        if (height == null)
            height = window.innerHeight;
        var chMgmtPrimary = document.getElementById("main-grid");
        var navbarHeight = document.getElementById("main-navbar").offsetHeight;
        var footerHeight = document.getElementById("footer").offsetHeight;
        var chDetailHeight = this.channel ? chDetailHeight = document.getElementById("chan-detail").offsetHeight : 0;
        if (chDetailHeight > (height * .2))
            chDetailHeight = height * .2;
        chMgmtPrimary.style.height = (height * .81 - footerHeight - navbarHeight - (chDetailHeight)).toString() + "px";
    };
    ChannelComponent.prototype.getUri = function (bitmapId) {
        if (!bitmapId)
            return;
        return "ChannelLogoRepository/" + bitmapId.toString() + ".png";
    };
    ChannelComponent.prototype.loadChannel = function (fiosid, region) {
        var _this = this;
        this.logger.log("loadChannel", fiosid);
        this._channelService.getBy('api/channel/', fiosid)
            .subscribe(function (x) {
            _this.channel = x.filter(function (y) { return y.strFIOSRegionName == region; }).pop();
        }, function (error) {
            _this.msg = error;
        }, function () {
            if (_this.updateChannel) {
                _this.logger.log('Row updated for fios id.', fiosid);
                var rowNodes_1 = [];
                _this.gridOptions.api.forEachNodeAfterFilterAndSort(function (rn) {
                    if (rn.data.id == _this.channel.strFIOSServiceId) {
                        rn.updateData({
                            id: _this.channel.strFIOSServiceId,
                            call: _this.channel.strStationCallSign,
                            name: _this.channel.strStationName,
                            num: rn.data.num,
                            region: rn.data.region,
                            logoid: rn.data.logoid
                        });
                        rowNodes_1.push(rn);
                    }
                });
                if (_this.editLogoForm.isSuccess && rowNodes_1.length > 0)
                    _this.gridOptions.api.redrawRows({ rowNodes: rowNodes_1 });
                _this.updateChannel = false;
            }
        });
    };
    ChannelComponent.prototype.loadVhos = function () {
        var _this = this;
        this.logger.log("loadVhos");
        this._channelService.get('api/region/vho')
            .subscribe(function (x) {
            _this.vhos = x;
        }, function (error) { return _this.msg = error; }, function () {
            _this.logger.log("loadVhos => finally");
            if (!_this.vho && _this.vhos)
                _this.vhoSelect(_this.vhos[0]);
        });
    };
    ChannelComponent.prototype.updateLogoCell = function ($id) {
        var _this = this;
        var rowNode = this.gridOptions.api.getRowNode($id);
        this.gridOptions.api.forEachNodeAfterFilterAndSort(function (node) {
            var fiosid = _this.gridOptions.api.getValue('id', node);
            if (fiosid != $id)
                return;
            var regionName = _this.gridOptions.api.getValue('region', node);
            _this._channelService.getBriefBy($id)
                .subscribe(function (ch) {
                node.setData(ch);
            }, function (error) { return _this.msg = error; }, function () {
                _this.gridOptions.api.refreshCells({ rowNodes: [node], columns: ['logo'], force: true, volatile: false });
                if (node.isSelected()) {
                    _this._channelLogoService.openLocalImage(_this.editLogoForm.newImage, function (img) {
                        _this.editLogoForm.imgSource = img;
                        _this.editLogoForm.newImage = undefined;
                    });
                }
                _this.loadChannel($id, regionName);
            });
        });
    };
    return ChannelComponent;
}());
__decorate([
    core_1.ViewChild(editlogo_component_1.EditLogoForm),
    __metadata("design:type", editlogo_component_1.EditLogoForm)
], ChannelComponent.prototype, "editLogoForm", void 0);
__decorate([
    core_1.HostListener('window:resize', ['$event']),
    __metadata("design:type", Function),
    __metadata("design:paramtypes", [Object]),
    __metadata("design:returntype", void 0)
], ChannelComponent.prototype, "onWindowResize", null);
ChannelComponent = __decorate([
    core_1.Component({
        selector: 'channel-manager',
        templateUrl: 'app/Components/channel.component.html',
    }),
    __metadata("design:paramtypes", [channel_service_1.ChannelService, channellogo_service_1.ChannelLogoService, default_logger_service_1.Logger])
], ChannelComponent);
exports.ChannelComponent = ChannelComponent;
function actionCellRenderer(params) {
    var htmlElements = '<button type="button" class="btn btn-primary btn-xs btn-edit-img" data-action-type="editlogo" title="Edit" (click)="editChannel(channel.id)">Edit</button>';
    return htmlElements;
}
function defaultCellRenderer(params) {
    return '<span class="renderedCell">' + params.value + '</span>';
}
