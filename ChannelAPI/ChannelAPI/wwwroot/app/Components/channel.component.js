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
const forms_1 = require("@angular/forms");
const ng2_bs3_modal_1 = require("ng2-bs3-modal/ng2-bs3-modal");
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
    constructor(fb, _channelService, _orderBy) {
        this.fb = fb;
        this._channelService = _channelService;
        this._orderBy = _orderBy;
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
    LoadChannels() {
        console.log("LoadChannels() called");
        this.indLoading = true;
        try {
            if (this.region != null && this.region.length > 0 && this.region.toLowerCase() != 'none')
                this.obsChannels = this._channelService.getBy('api/channel/region/', this.region);
            else {
                this.obsChannels = this._channelService.getBy('api/channel/vho/', this.vho);
            }
        }
        catch (ex) {
            console.error(ex);
            throw ex;
        }
        this.indLoading = false;
    }
    getUri(bitmapId) {
        if (!bitmapId)
            return;
        return "/ChannelLogoRepository/" + bitmapId.toString() + ".png";
    }
    onSort(val, event) {
        console.log('onSort called. param: ' + val);
        try {
            var target = event.target || event.srcElement || event.currentTarget;
            if (this.channelsBrief == null)
                return;
            this._orderBy.transform(this.channelsBrief, [val]);
        }
        catch (ex) {
            console.error(ex);
            this.msg = ex;
        }
    }
};
__decorate([
    core_1.ViewChild('modal'),
    __metadata("design:type", ng2_bs3_modal_1.ModalComponent)
], ChannelComponent.prototype, "modal", void 0);
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
    __metadata("design:paramtypes", [forms_1.FormBuilder, channel_service_1.ChannelService, order_by_pipe_1.OrderBy])
], ChannelComponent);
exports.ChannelComponent = ChannelComponent;
function logoCellRenderer(params) {
    var htmlElements = '<div class="bg-black3d">';
    htmlElements += '<img src="/ChannelLogoRepository/' + params.value + '.png" alt="Loading" />';
    htmlElements += "<div class='editBtnWrapper'>";
    htmlElements += '<button type="button" class="btn btn-primary btn-xs" title="Edit" (click)="editChannel(channel.id)">Edit</button>';
    htmlElements += '</div></div>';
    return htmlElements;
}
function actionCellRenderer(params) {
    var htmlElements = '<button type="button" class="btn btn-primary btn-xs" title="Edit" (click)="editChannel(channel.id)">Edit</button>';
    return htmlElements;
}
function defaultCellRenderer(params) {
    return '<span class="renderedCell">' + params.value + '</span>';
}
//# sourceMappingURL=channel.component.js.map