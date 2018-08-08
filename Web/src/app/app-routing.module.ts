import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { TopAdvisorsComponent } from './components/advisor/top-advisors/top-advisors.component';
import { LoginComponent } from './components/account/login/login.component';
import { AuthGuard } from './providers/authGuard';
import { AuthRedirect } from './providers/authRedirect';
import { ConfirmEmailComponent } from './components/account/confirm-email/confirm-email.component';
import { MessageSignatureComponent } from './components/account/message-signature/message-signature.component';

const routes: Routes = [
    { path: '', redirectTo: 'feed', pathMatch: 'full' },
    { path: 'top-advisors', component: TopAdvisorsComponent, canActivate: [AuthRedirect]  },
    { path: 'login', component: LoginComponent },
    { path: 'feed', component: LoginComponent, canActivate: [AuthRedirect],  },
    { path: 'confirm-email', component: ConfirmEmailComponent, canActivate: [AuthRedirect] },
    { path: 'wallet-login', component: MessageSignatureComponent, canActivate: [AuthRedirect] },
];

@NgModule({
    exports: [RouterModule],
    imports: [RouterModule.forRoot(routes)]
})

export class AppRoutingModule { }
