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
const forms_1 = require("@angular/forms");
const order_by_pipe_1 = require("../order-by.pipe");
require("rxjs/add/operator/distinctUntilChanged");
require("rxjs/add/operator/distinct");
require("rxjs/add/operator/map");
require("rxjs/add/operator/switchMap");
require("rxjs/add/operator/do");
require("rxjs/add/operator/catch");
require("rxjs/add/observable/of");
require("rxjs/add/observable/from");
require("rxjs/add/operator/toArray");
const _ = require("lodash");
const header_component_1 = require("./header.component");
let ChannelComponent = class ChannelComponent {
    constructor(fb, _channelService, _channelLogoService) {
        this.fb = fb;
        this._channelService = _channelService;
        this._channelLogoService = _channelLogoService;
        this.indLoading = false;
        this.imgLoading = false;
        console.log("ChannelComponent constructor called");
        this.gridOptions = {};
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
        this.channelFrm = this.fb.group({
            Id: [''],
            CallSign: [''],
            StationName: [''],
            BitMapId: ['10000']
        });
        this.vho = '1';
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
            this.gridOptions.api.setRowData(_.uniqBy(chb, 'num'));
        }, error => this.msg = error);
    }
    createColumnDefs() {
        console.log("createColumnDefs called");
        this.columnDefs = [
            {
                headerName: 'Service ID',
                field: 'id',
                unSortIcon: false,
                cellClass: function (params) { return (params.value = 'center-cell'); },
                cellRenderer: defaultCellRenderer
            },
            {
                headerName: 'Channel #',
                field: 'num',
                sort: 'asc',
                unSortIcon: false,
                cellClass: function (params) { return (params.value = 'center-cell'); },
                cellRenderer: defaultCellRenderer
            },
            {
                headerName: 'Call Sign',
                field: 'call',
                unSortIcon: false,
                cellClass: function (params) { return (params.value = 'center-cell'); },
                cellRenderer: defaultCellRenderer
            },
            {
                headerName: 'Station Name',
                field: 'name',
                unSortIcon: false,
                cellClass: function (params) { return (params.value = 'center-cell'); },
                cellRenderer: defaultCellRenderer
            },
            {
                headerName: 'Region',
                field: 'region',
                unSortIcon: false,
                cellClass: function (params) { return (params.value = 'center-cell'); },
                cellRenderer: defaultCellRenderer
            },
            {
                headerName: 'Logo',
                cellRenderer: logoCellRenderer,
                field: 'logoid',
                suppressFilter: true,
                suppressSorting: true,
            }
        ];
    }
    onReady() {
        console.log('onReady');
        this.createRowData();
        this.createColumnDefs();
        this.gridOptions.columnApi.autoSizeAllColumns();
        this.gridOptions.onGridSizeChanged = this.onGridSizeChanged;
    }
    onRowSelected($event) {
        console.log("onRowSelected:" + $event.node.data.name);
    }
    onFilterModified() {
        console.log("onFilterModified");
    }
    onRowClicked($event) {
        console.log("onRowClicked: " + $event.node.data.name);
        if ($event.event.target !== undefined) {
            let data = $event.data;
            let actionType = $event.event.target.getAttribute("data-action-type");
            switch (actionType) {
                case "editlogo":
                    return this.editChannelLogo($event.node.data.id);
            }
        }
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
    columnResize() {
        console.log("columnResize");
        if (this.gridOptions.api && this.columnDefs) {
            this.gridOptions.api.sizeColumnsToFit();
        }
    }
    onWindowResize(event) {
        console.log("onWindowsResize");
        let windowHeight = event.target.innerHeight;
        var chMgmtPrimary = document.getElementById("main-grid");
        var navbarHeight = document.getElementById("main-navbar").offsetHeight;
        var footerHeight = document.getElementById("footer").offsetHeight;
        if (this.channel) {
        }
        else {
            chMgmtPrimary.style.height = (windowHeight - footerHeight - navbarHeight - (windowHeight * .2)).toString() + "px";
        }
    }
    getUri(bitmapId) {
        if (!bitmapId)
            return;
        return "/ChannelLogoRepository/" + bitmapId.toString() + ".png";
    }
    editChannelLogo(fiosid) {
        console.log("editChannelLogo({0})", fiosid);
        this._channelService.getBy('api/channel/', fiosid).subscribe(x => {
            this.channel = x;
        });
        this.editLogoForm.showForm = true;
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
        selector: 'home',
        templateUrl: 'app/Components/channel.component.html',
        providers: [order_by_pipe_1.OrderBy]
    }),
    __metadata("design:paramtypes", [forms_1.FormBuilder, channel_service_1.ChannelService, channellogo_service_1.ChannelLogoService])
], ChannelComponent);
exports.ChannelComponent = ChannelComponent;
function logoCellRenderer(params) {
    var htmlElements = '<div class="bg-black3d">';
    htmlElements += '<img src="/ChannelLogoRepository/' + params.value + '.png" alt="Loading" />';
    htmlElements += "<div class='editBtnWrapper'>";
    htmlElements += '<button type="button" class="btn btn-primary btn-xs" title="Edit" data-action-type="editlogo">Edit</button>';
    htmlElements += '</div></div>';
    return htmlElements;
}
function actionCellRenderer(params) {
    var htmlElements = '<button type="button" class="btn btn-primary btn-xs" data-action-type="editlogo" title="Edit" (click)="editChannel(channel.id)">Edit</button>';
    return htmlElements;
}
function defaultCellRenderer(params) {
    return '<span class="renderedCell">' + params.value + '</span>';
}
//# sourceMappingURL=channel.component.js.map