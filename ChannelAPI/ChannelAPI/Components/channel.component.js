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
const editlogo_component_1 = require("./editlogo.component");
require("rxjs/add/operator/map");
require("rxjs/add/operator/do");
require("rxjs/add/operator/catch");
require("rxjs/add/operator/finally");
const _ = require("lodash");
const header_component_1 = require("./header.component");
const imagecellrender_component_1 = require("./imagecellrender.component");
let ChannelComponent = class ChannelComponent {
    constructor(_channelService, _channelLogoService) {
        this._channelService = _channelService;
        this._channelLogoService = _channelLogoService;
        this.indLoading = false;
        this.imgLoading = false;
        this.updateChannel = false;
        this.showChannelInfo = true;
        console.log("ChannelComponent constructor called");
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
    ngOnInit() {
        console.log("channelcomponent: ngOnInit() called");
        this.loadVhos();
    }
    ngAfterViewInit() {
        console.log("ngAfterViewInit called");
    }
    createRowData() {
        console.log("createRowData() called");
        this._channelService.getBy('api/channel/vho/', this.vho).map(arr => arr.map(ch => {
            return { id: ch.strFIOSServiceId, call: ch.strStationCallSign, name: ch.strStationName, num: ch.intChannelPosition, region: ch.strFIOSRegionName, logoid: ch.intBitMapId };
        }))
            .subscribe(chb => {
            this.gridOptions.api.setRowData(_.uniqBy(chb, function (ch) { return [ch.num, ch.region].join(); }));
            this.gridOptions.api.hideOverlay();
        }, error => this.msg = error);
    }
    createColumnDefs() {
        console.log("createColumnDefs called");
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
    }
    onReady() {
        console.log('onReady');
        this.gridOptions.api.showLoadingOverlay();
        this.createColumnDefs();
        this.gridOptions.columnApi.autoSizeAllColumns();
        this.gridOptions.onGridSizeChanged = this.onGridSizeChanged;
        this.flexWidth(window.innerWidth);
        this.fitGridHeight(window.innerHeight);
    }
    onRowSelected($event) {
        console.log("onRowSelected:" + $event.node.data.name);
    }
    onFilterModified() {
        console.log("onFilterModified");
    }
    onRowClicked($event) {
        console.log("onRowClicked: " + $event.node.data.name);
        $event.node.setSelected(true, true);
        if ($event.event.target !== undefined) {
            let data = $event.data;
            let actionType = $event.event.target.getAttribute("data-action-type");
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
    }
    toggleChannelInfo($event) {
        this.showChannelInfo = !this.showChannelInfo;
    }
    onQuickFilterChanged($event) {
        this.gridOptions.api.setQuickFilter($event.target.value);
    }
    onModelUpdated() {
        console.log("onModelUpdated");
        if (this.gridOptions.api && this.columnDefs) {
            this.gridOptions.api.sizeColumnsToFit();
        }
    }
    onGridSizeChanged($event) {
        console.log("gridSizeChanged");
        if ($event && $event.api) {
            $event.api.sizeColumnsToFit();
        }
    }
    onVhoSelect($event) {
        this.gridOptions.api.showLoadingOverlay();
        if (this.channel)
            this.channel = undefined;
        this.vhoSelect($event.target.value);
    }
    vhoSelect(vhoName) {
        var vho = vhoName.toLowerCase().startsWith('vho') ? vhoName.toLowerCase().replace('vho', '') : vhoName;
        if (this.vho != vho)
            if (this.vho != vho) {
                this.vho = vho;
                this.createRowData();
                this.gridOptions.api.redrawRows();
            }
    }
    columnResize() {
        console.log("columnResize");
        if (this.gridOptions.api && this.columnDefs) {
            this.gridOptions.api.sizeColumnsToFit();
        }
    }
    updateRow($event) {
        console.log("updateRow called", $event);
        this.updateChannel = true;
        this.loadChannel($event, this.channel.strFIOSRegionName);
    }
    onWindowResize(event) {
        console.log("onWindowsResize");
        let windowHeight = event.target.innerHeight;
        this.flexWidth(window.innerWidth);
        this.fitGridHeight(windowHeight);
    }
    flexWidth(width) {
        console.log("flexWidth", width);
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
    }
    fitGridHeight(height) {
        console.log("fitGridHeight", height);
        if (height == null)
            height = window.innerHeight;
        var chMgmtPrimary = document.getElementById("main-grid");
        var navbarHeight = document.getElementById("main-navbar").offsetHeight;
        var footerHeight = document.getElementById("footer").offsetHeight;
        var chDetailHeight = this.channel ? chDetailHeight = document.getElementById("chan-detail").offsetHeight : 0;
        if (chDetailHeight > (height * .2))
            chDetailHeight = height * .2;
        chMgmtPrimary.style.height = (height * .81 - footerHeight - navbarHeight - (chDetailHeight)).toString() + "px";
    }
    getUri(bitmapId) {
        if (!bitmapId)
            return;
        return "/ChannelLogoRepository/" + bitmapId.toString() + ".png";
    }
    loadChannel(fiosid, region) {
        console.log("loadChannel", fiosid);
        this._channelService.getBy('api/channel/', fiosid)
            .finally(() => {
            if (this.updateChannel) {
                console.log('Row updated for fios id.', fiosid);
                var rowNodes = this.gridOptions.api.getSelectedNodes();
                if (!rowNodes || rowNodes.length < 1) {
                    this.updateChannel = false;
                    return;
                }
                var rowNode = rowNodes[0];
                rowNode.updateData({
                    id: this.channel.strFIOSServiceId,
                    call: this.channel.strStationCallSign,
                    name: this.channel.strStationName,
                    num: this.channel.intChannelPosition,
                    region: this.channel.strFIOSRegionName,
                    logoid: this.channel.intBitMapId
                });
                if (this.editLogoForm.isSuccess)
                    this.gridOptions.api.redrawRows({ rowNodes: rowNodes });
                this.updateChannel = false;
            }
        })
            .subscribe((x) => {
            this.channel = x.filter(y => y.strFIOSRegionName == region).pop();
        });
    }
    loadVhos() {
        console.log("loadVhos");
        this._channelService.get('api/region/vho')
            .finally(() => {
            console.log("loadVhos => finally");
            if (!this.vho && this.vhos)
                this.vhoSelect(this.vhos[0]);
        })
            .subscribe((x) => {
            this.vhos = x;
        }, error => this.msg = error);
    }
    updateLogoCell($id) {
        var rowNode = this.gridOptions.api.getRowNode($id);
        this.gridOptions.api.forEachNodeAfterFilterAndSort((node) => {
            var fiosid = this.gridOptions.api.getValue('id', node);
            if (fiosid != $id)
                return;
            var regionName = this.gridOptions.api.getValue('region', node);
            this._channelService.getBriefBy($id)
                .subscribe((ch) => {
                node.setData(ch);
            }, (error) => this.msg = error, () => {
                this.gridOptions.api.refreshCells({ rowNodes: [node], columns: ['logo'], force: true, volatile: false });
                if (node.isSelected()) {
                    this._channelLogoService.openLocalImage(this.editLogoForm.newImage, (img) => {
                        this.editLogoForm.imgSource = img;
                        this.editLogoForm.newImage = undefined;
                    });
                }
                this.loadChannel($id, regionName);
            });
        });
    }
};
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
    __metadata("design:paramtypes", [channel_service_1.ChannelService, channellogo_service_1.ChannelLogoService])
], ChannelComponent);
exports.ChannelComponent = ChannelComponent;
function actionCellRenderer(params) {
    var htmlElements = '<button type="button" class="btn btn-primary btn-xs btn-edit-img" data-action-type="editlogo" title="Edit" (click)="editChannel(channel.id)">Edit</button>';
    return htmlElements;
}
function defaultCellRenderer(params) {
    return '<span class="renderedCell">' + params.value + '</span>';
}
