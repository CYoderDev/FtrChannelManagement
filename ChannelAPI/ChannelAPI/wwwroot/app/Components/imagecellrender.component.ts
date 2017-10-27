import { Component, ViewChild, ElementRef } from '@angular/core'
import { ICellRendererAngularComp } from 'ag-grid-angular';
import { Observable } from 'rxjs/Observable';
import { ChannelLogoService } from '../Service/channellogo.service';

@Component({
    selector: 'logo-cell',
    template: `
            <div class="bg-black3d">
                <div *ngIf="!source" class="fa fa-2x fa-spinner fa-pulse" aria-hidden="true"></div>
                <img *ngIf="source" #logo src="{{source}}" alt="No Logo" />
                <div class='editBtnWrapper'>
                    <button type="button" class="btn btn-primary btn-xs" title="Edit" data-action-type="editlogo">Edit</button>
                </div>
            </div>
`
})

export class ImageCellRendererComponent implements ICellRendererAngularComp {
    private params: any;
    private source: string;
    private sourceBase: string = 'ChannelLogoRepository/';
    @ViewChild('logo') logoEle: ElementRef;
    private image;

    constructor(private _channelLogoService: ChannelLogoService)
    {
        
    }

    agInit(params: any): void {
        this.params = params;
        this.source = this.sourceBase + this.params.value + '.png';
    }

    refresh(params: any): boolean {
        this.source = undefined;
        this.params = params;
        //Cache buster to get the image to display immediately in the current view.
        //It will reset back to the original source as soon as the image is scrolled out of view
        this.logoEle.nativeElement.src = this.sourceBase + params.value + '.png?' + new Date().getTime();
        this.source = this.sourceBase + params.value + '.png?' + new Date().getTime();
        ImageCellRendererComponent.getLogo(this.params.value, this._channelLogoService)
            .subscribe((img) =>
            {
                this.image = img;
            });
        return true;
    }

    //Forces the browser cache to update the existing image if it has been modified
    public static getLogo(id: number, logoService: ChannelLogoService) {
        return logoService.performRequest('ChannelLogoRepository/' + id + '.png', 'GET', null, 'application/json');
    }
}