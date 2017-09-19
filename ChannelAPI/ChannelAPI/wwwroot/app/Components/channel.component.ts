import { Component, OnInit, ViewChild, ViewContainerRef, AfterViewInit, HostListener } from '@angular/core';
import { ChannelService } from '../Service/channel.service';
import { ChannelLogoService } from '../Service/channellogo.service'
import { ChannelLogoComponent } from './channellogo.component';
import { EditLogoForm } from './editlogo.component'
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ModalComponent } from 'ng2-bs3-modal/ng2-bs3-modal';
import { IChannel } from '../Models/channel';
import { OrderBy } from '../order-by.pipe';
import 'rxjs/add/operator/distinctUntilChanged';
import 'rxjs/add/operator/distinct';
import { Observable } from 'rxjs/Observable';
import 'rxjs/add/operator/map';
import 'rxjs/add/operator/switchMap';
import 'rxjs/add/operator/do';
import 'rxjs/add/operator/catch';
import 'rxjs/add/observable/of';
import 'rxjs/add/observable/from';
import 'rxjs/add/operator/toArray';
import { BehaviorSubject } from 'rxjs/BehaviorSubject';
import * as _ from 'lodash';
import { GridOptions } from 'ag-grid/main';
import { HeaderComponent } from './header.component';

@Component({
    selector: 'home',
    templateUrl: 'app/Components/channel.component.html',
    providers: [OrderBy]
})

export class ChannelComponent implements OnInit, AfterViewInit
{
    @ViewChild(EditLogoForm) editLogoForm: EditLogoForm;
    channels: IChannel[];
    channel: IChannel;
    channelsBrief: any;
    vho: string;
    region: string;
    channelFrm: FormGroup;
    indLoading: boolean = false;
    imgLoading: boolean = false;
    msg: string;
    logo: any

    private obsChannels: Observable<IChannel[]>;
    private gridOptions: GridOptions;
    public rowData: any[];
    public showGrid: boolean;
    private columnDefs: any[];
    
    constructor(private fb: FormBuilder, private _channelService: ChannelService, private _channelLogoService: ChannelLogoService) {
        console.log("ChannelComponent constructor called");
        this.gridOptions = <GridOptions>{
 
        };

        this.showGrid = true;
        this.gridOptions.defaultColDef = {
            headerComponentFramework: <{ new (): HeaderComponent }>HeaderComponent,
            headerComponentParams: {
                menuIcon: 'fa-bars'
            }
        }
    }

    ngOnInit(): void {
        console.log("channelcomponent: ngOnInit() called");
        this.channelFrm = this.fb.group({
            Id: [''],
            CallSign: [''],
            StationName: [''],
            BitMapId: ['10000']
        });
        this.vho = '1';
    }

    ngAfterViewInit(): void {
        console.log("ngAfterViewInit called");
    }

    private createRowData() {
        console.log("createRowData() called");

        this._channelService.getBy('api/channel/vho/', this.vho).map(arr => arr.map(ch => {
            return { id: ch.strFIOSServiceId, call: ch.strStationCallSign, name: ch.strStationName, num: ch.intChannelPosition, region: ch.strFIOSRegionName, logoid: ch.intBitMapId };
        }))
            .subscribe(chb => {
                this.gridOptions.api.setRowData(_.uniqBy(chb, 'num'));
            }, error => this.msg = <any>error);
        
    }

    private createColumnDefs() {
        console.log("createColumnDefs called");
        this.columnDefs = [
            {
                headerName: 'Service ID',
                field: 'id',
                unSortIcon: false,
                cellClass: function (params) { return (params.value = 'center-cell') },
                cellRenderer: defaultCellRenderer
            },
            {
                headerName: 'Channel #',
                field: 'num',
                sort: 'asc',
                unSortIcon: false,
                cellClass: function (params) { return (params.value = 'center-cell') },
                cellRenderer: defaultCellRenderer
            },
            {
                headerName: 'Call Sign',
                field: 'call',
                unSortIcon: false,
                cellClass: function (params) { return (params.value = 'center-cell') },
                cellRenderer: defaultCellRenderer
            },
            {
                headerName: 'Station Name',
                field: 'name',
                unSortIcon: false,
                cellClass: function (params) { return (params.value = 'center-cell') },
                cellRenderer: defaultCellRenderer
            },
            {
                headerName: 'Region',
                field: 'region',
                unSortIcon: false,
                cellClass: function (params) { return (params.value = 'center-cell') },
                cellRenderer: defaultCellRenderer
            },
            {
                headerName: 'Logo',
                cellRenderer: logoCellRenderer,
                field: 'logoid',
                suppressFilter: true,
                suppressSorting: true,
            }
        ]
    }

    private onReady() {
        console.log('onReady');
        this.createRowData();
        this.createColumnDefs();
        this.gridOptions.columnApi.autoSizeAllColumns();
        this.gridOptions.onGridSizeChanged = this.onGridSizeChanged;
    }

    private onRowSelected($event) {
        console.log("onRowSelected:" + $event.node.data.name);
    }

    private onFilterModified() {
        console.log("onFilterModified");
    }

    private onRowClicked($event) {
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

    public onQuickFilterChanged($event) {
        this.gridOptions.api.setQuickFilter($event.target.value);
    }

    private onModelUpdated() {
        console.log("onModelUpdated");
        if (this.gridOptions.api && this.columnDefs) {
            this.gridOptions.api.sizeColumnsToFit();
        }
    }

    private onGridSizeChanged($event) {
        console.log("gridSizeChanged");
        if ($event && $event.api)
        {
            $event.api.sizeColumnsToFit();
        }
    }

    private columnResize() {
        console.log("columnResize");
        if (this.gridOptions.api && this.columnDefs) {
            this.gridOptions.api.sizeColumnsToFit();
        }
    }

    @HostListener('window:resize', ['$event'])
    onWindowResize(event)
    {
        console.log("onWindowsResize");
        let windowHeight = event.target.innerHeight;
        var chMgmtPrimary = document.getElementById("main-grid");
        var navbarHeight = document.getElementById("main-navbar").offsetHeight;
        var footerHeight = document.getElementById("footer").offsetHeight;

        if (this.channel)
        {

        }
        else
        {
            chMgmtPrimary.style.height = (windowHeight - footerHeight - navbarHeight - (windowHeight * .2)).toString() + "px";
        }
    }

    getUri(bitmapId: number) {
        if (!bitmapId)
            return;
        return "/ChannelLogoRepository/" + bitmapId.toString() + ".png";
    }

    editChannelLogo(fiosid: string) {
        console.log("editChannelLogo({0})", fiosid);
        this._channelService.getBy('api/channel/', fiosid).subscribe(x => {
            this.channel = x;
        });

        //var stations;

        //this._channelLogoService.getBy('api/channellogo/{0}/stations', this.channel.intBitMapId).subscribe(x => {
        //    stations = x;
        //});

        ////Notify user that there are multiple stations assigned to this bitmap id.
        ////User can either select to assign a new image to all stations, or just the selected station.
        //if (stations && stations.length > 0)
        //{

        //}

        this.editLogoForm.showForm = true;
    }
}

function logoCellRenderer(params) {
    var htmlElements = '<div class="bg-black3d">';
    htmlElements += '<img src="/ChannelLogoRepository/' + params.value + '.png" alt="Loading" />';
    htmlElements += "<div class='editBtnWrapper'>"
    htmlElements += '<button type="button" class="btn btn-primary btn-xs" title="Edit" data-action-type="editlogo">Edit</button>';
    htmlElements += '</div></div>';

    return htmlElements;
}

function actionCellRenderer(params) {
    var htmlElements = '<button type="button" class="btn btn-primary btn-xs" data-action-type="editlogo" title="Edit" (click)="editChannel(channel.id)">Edit</button>';
    return htmlElements;
}

function defaultCellRenderer(params) {
    return '<span class="renderedCell">' + params.value + '</span>'
}