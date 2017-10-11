import { Component, OnInit, ViewChild, ViewContainerRef, AfterViewInit, HostListener } from '@angular/core';
import { ChannelService } from '../Service/channel.service';
import { ChannelLogoService } from '../Service/channellogo.service'
import { EditLogoForm } from './editlogo.component'
import { IChannel } from '../Models/channel';
import { Observable } from 'rxjs/Observable';
import 'rxjs/add/operator/map';
import 'rxjs/add/operator/do';
import 'rxjs/add/operator/catch';
import 'rxjs/add/operator/finally';
import * as _ from 'lodash';
import { GridOptions } from 'ag-grid/main';
import { HeaderComponent } from './header.component';
import { ImageCellRendererComponent } from './imagecellrender.component';

@Component({
    selector: 'channel-manager',
    templateUrl: 'app/Components/channel.component.html',
})

export class ChannelComponent implements OnInit, AfterViewInit
{
    @ViewChild(EditLogoForm) editLogoForm: EditLogoForm;
    channels: IChannel[];
    channel: IChannel;
    channelsBrief: any;
    vhos: {};
    vho: string;
    indLoading: boolean = false;
    imgLoading: boolean = false;
    private updateChannel: boolean = false;
    showChannelInfo: boolean = true;
    msg: string;
    logo: any

    private obsChannels: Observable<IChannel[]>;
    private gridOptions: GridOptions;
    public rowData: any[];
    public showGrid: boolean;
    private columnDefs: any[];
    
    constructor(private _channelService: ChannelService, private _channelLogoService: ChannelLogoService) {
        console.log("ChannelComponent constructor called");
        this.gridOptions = <GridOptions>{
            getRowNodeId: function (data) { return data.id + data.region + data.num }
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
        this.loadVhos();
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
                this.gridOptions.api.setRowData(_.uniqBy(chb, function (ch: any) { return [ch.num, ch.region].join()}));
                this.gridOptions.api.hideOverlay();
            }, error => this.msg = <any>error);
        
    }

    private createColumnDefs() {
        console.log("createColumnDefs called");
        this.columnDefs = [
            {
                headerName: 'Service ID',
                field: 'id',
                minWidth: 115,
                unSortIcon: false,
                cellClass: function (params) { return (params.value = 'center-cell') },
                cellRenderer: defaultCellRenderer,
            },
            {
                headerName: 'Channel #',
                field: 'num',
                minWidth: 112,
                sort: 'asc',
                unSortIcon: false,
                cellClass: function (params) { return (params.value = 'center-cell') },
                cellRenderer: defaultCellRenderer
            },
            {
                headerName: 'Call Sign',
                field: 'call',
                minWidth: 105,
                unSortIcon: false,
                cellClass: function (params) { return (params.value = 'center-cell') },
                cellRenderer: defaultCellRenderer
            },
            {
                headerName: 'Station Name',
                field: 'name',
                unSortIcon: false,
                minWidth: 135,
                cellClass: function (params) { return (params.value = 'center-cell') },
                cellRenderer: defaultCellRenderer
            },
            {
                headerName: 'Region',
                field: 'region',
                minWidth: 95,
                unSortIcon: false,
                cellClass: function (params) { return (params.value = 'center-cell') },
                cellRenderer: defaultCellRenderer
            },
            {
                headerName: 'Logo',
                cellRendererFramework: ImageCellRendererComponent,
                field: 'logoid',
                minWidth: 90,
                colId: 'logo',
                suppressFilter: true,
                suppressSorting: true,
            }
        ]
    }

    private onReady() {
        console.log('onReady');
        this.gridOptions.api.showLoadingOverlay();
        this.createColumnDefs();
        this.gridOptions.columnApi.autoSizeAllColumns();
        this.gridOptions.onGridSizeChanged = this.onGridSizeChanged;
        this.flexWidth(window.innerWidth);
        this.fitGridHeight(window.innerHeight);
    }

    private onRowSelected($event) {
        console.log("onRowSelected:" + $event.node.data.name);
    }

    private onFilterModified() {
        console.log("onFilterModified");
    }

    private onRowClicked($event) {
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

    private toggleChannelInfo($event) {
        this.showChannelInfo = !this.showChannelInfo;
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

    private onVhoSelect($event) {
        this.gridOptions.api.showLoadingOverlay();
        if (this.channel)
            this.channel = undefined;
        this.vhoSelect($event.target.value);
    }

    private vhoSelect(vhoName: string)
    {
        var vho = vhoName.toLowerCase().startsWith('vho') ? vhoName.toLowerCase().replace('vho', '') : vhoName;
        if (this.vho != vho)
            if (this.vho != vho) {
                this.vho = vho;
                this.createRowData();
                this.gridOptions.api.redrawRows();
            }
    }

    private columnResize() {
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

    @HostListener('window:resize', ['$event'])
    onWindowResize(event)
    {
        console.log("onWindowsResize");
        let windowHeight = event.target.innerHeight;
        this.flexWidth(window.innerWidth);
        this.fitGridHeight(windowHeight);
        //var chMgmtPrimary = document.getElementById("main-grid");
        //var navbarHeight = document.getElementById("main-navbar").offsetHeight;
        //var footerHeight = document.getElementById("footer").offsetHeight;

        //chMgmtPrimary.style.height = (windowHeight - footerHeight - navbarHeight - (windowHeight * .2)).toString() + "px";
    }

    private flexWidth(width: number) {
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

    private fitGridHeight(height?: number) {
        console.log("fitGridHeight", height);

        if (height == null)
            height = window.innerHeight;

        var chMgmtPrimary = document.getElementById("main-grid");
        var navbarHeight = document.getElementById("main-navbar").offsetHeight;
        var footerHeight = document.getElementById("footer").offsetHeight;
        var chDetailHeight = this.channel ? chDetailHeight = document.getElementById("chan-detail").offsetHeight : 0;

        if (chDetailHeight > (height * .2))
            chDetailHeight = height * .2;
        
        chMgmtPrimary.style.height = (height * .81 - footerHeight - navbarHeight - (chDetailHeight) ).toString() + "px";
    }

    getUri(bitmapId: number) {
        if (!bitmapId)
            return;
        return "/ChannelLogoRepository/" + bitmapId.toString() + ".png";
    }

    loadChannel(fiosid: string, region: string) {
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
                    rowNode.updateData(
                        {
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
            .subscribe((x: IChannel[]) =>
            {
                //Assign channel to the first channel that belongs to the selected region
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
            .subscribe((x: string) => {
                this.vhos = x;
            }, error => this.msg = <any>error);
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
                })
        })
        //if (rowNode) {
        //    this._channelService.getBriefBy($id)
        //        .subscribe((ch) => {
        //            rowNode.setData(ch);
        //        }, (error) => this.msg = error, () => {
        //            this.gridOptions.api.refreshCells({ rowNodes: [rowNode], columns: ['logo'], force: true, volatile: false });
        //            if (rowNode.isSelected()) {
        //                this._channelLogoService.openLocalImage(this.editLogoForm.newImage, (img) => {
        //                    this.editLogoForm.imgSource = img;
        //                    this.editLogoForm.newImage = undefined;
        //                });
        //                this.loadChannel($id, this.channel.strFIOSRegionName);
        //            }
        //        })
        //}
    }
}

function actionCellRenderer(params) {
    var htmlElements = '<button type="button" class="btn btn-primary btn-xs btn-edit-img" data-action-type="editlogo" title="Edit" (click)="editChannel(channel.id)">Edit</button>';
    return htmlElements;
}

function defaultCellRenderer(params) {
    return '<span class="renderedCell">' + params.value + '</span>'
}