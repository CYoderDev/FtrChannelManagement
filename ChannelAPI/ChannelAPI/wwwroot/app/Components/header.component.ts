import { Component, ElementRef } from "@angular/core";
import { IHeaderParams } from "ag-grid/main"
import { IHeaderAngularComp } from "ag-grid-angular/main";

interface HeaderParams extends IHeaderParams {
    menuIcon: string;
}

@Component({
    templateUrl: 'app/Components/header.component.html',
    styleUrls: ['app/Components/header.component.css']
})

export class HeaderComponent implements IHeaderAngularComp {
    public params: HeaderParams;
    public sorted: string;
    private elementRef: ElementRef;

    constructor(elementRef: ElementRef) {
        console.log("HeaderComponent constructor");
        this.elementRef = elementRef;
    }

    agInit(params: HeaderParams): void {
        console.log("HeaderComponent agInit");
        this.params = params;
        this.params.column.addEventListener('sortChanged', this.onSortChanged.bind(this));
        this.onSortChanged();
    }

    ngOnDestroy() {
        console.log(`Destroying Header Component`);
    }

    onMenuClick() {
        this.params.showColumnMenu(this.querySelector('.customHeaderMenuButton'));
    }

    onSortRequested(order, event) {
        this.params.setSort(order, event.shiftKey);
    };

    onSortChanged() {
        if (this.params.column.isSortAscending()) {
            this.sorted = 'asc';
        }
        else if (this.params.column.isSortDescending()) {
            this.sorted = 'desc';
        }
        else {
            this.sorted = '';
        }
    };

    private querySelector(selector: string) {
        return <HTMLElement>this.elementRef.nativeElement.querySelector(
            '.customHeaderMenuButton', selector);
    }
}