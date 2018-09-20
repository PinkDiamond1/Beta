﻿using Auctus.DataAccess.Advisor;
using Auctus.DataAccessInterfaces.Advisor;
using Auctus.DomainObjects.Account;
using Auctus.DomainObjects.Advisor;
using Auctus.Util;
using Auctus.Util.Exceptions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Auctus.Business.Advisor
{
    public class RequestToBeAdvisorBusiness : BaseBusiness<RequestToBeAdvisor, IRequestToBeAdvisorData<RequestToBeAdvisor>>
    {
        public RequestToBeAdvisorBusiness(IConfigurationRoot configuration, IServiceProvider serviceProvider, IServiceScopeFactory serviceScopeFactory, ILoggerFactory loggerFactory, Cache cache, string email, string ip) : base(configuration, serviceProvider, serviceScopeFactory, loggerFactory, cache, email, ip) { }

        public RequestToBeAdvisor GetByUser(int userId)
        {
            return Data.GetByUser(userId);
        }

        public RequestToBeAdvisor GetByLoggedEmail()
        {
            if (string.IsNullOrWhiteSpace(LoggedEmail))
                return null;

            var user = UserBusiness.GetByEmail(LoggedEmail);
            if(user == null)
                throw new NotFoundException("Invalid email.");
            return Data.GetByUser(user.Id);
        }

        public List<RequestToBeAdvisor> ListPending()
        {
            return Data.ListPending();
        }

        public async Task ApproveAsync(int id)
        {
            var request = Data.GetById(id);
            var user = UserBusiness.GetById(id);
            if (user.IsAdvisor)
                throw new BusinessException("User is already advisor.");
            if (request.Approved == true)
                throw new BusinessException("Requets is already approved.");

            request.Approved = true;
            var urlGuid = request.UrlGuid ?? Guid.NewGuid();
            var advisor = AdvisorBusiness.CreateFromRequest(request, urlGuid);
            if (!request.UrlGuid.HasValue)
                await AzureStorageBusiness.UploadUserPictureFromBytesAsync($"{urlGuid}.png", AdvisorBusiness.GetNoUploadedImageForAdvisor(user));
            using (var transaction = TransactionalDapperCommand)
            {
                transaction.Insert(advisor);
                transaction.Update(request);
                transaction.Commit();
            }

            UserBusiness.ClearUserCache(user.Email);
            AdvisorBusiness.UpdateAdvisorsCacheAsync();

            await SendRequestApprovedNotificationAsync(user);
        }

        public async Task RejectAsync(int id)
        {
            var request = Data.GetById(id);
            var user = UserBusiness.GetById(request.UserId);

            request.Approved = false;
            Update(request);

            await SendRequestRejectedNotificationAsync(user);
        }

        public async Task<RequestToBeAdvisor> CreateAsync(string email, string password, string name, string description, string previousExperience, 
            bool changePicture, Stream pictureStream, string pictureExtension)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new BusinessException("Name must be filled.");
            if (name.Length > 50)
                throw new BusinessException("Name cannot have more than 50 characters.");
            if (string.IsNullOrWhiteSpace(description))
                throw new BusinessException("Short description must be filled.");
            if (description.Length > 160)
                throw new BusinessException("Short description cannot have more than 160 characters.");
            if (string.IsNullOrWhiteSpace(previousExperience))
                throw new BusinessException("Previous experience must be filled.");
            if (previousExperience.Length > 4000)
                throw new BusinessException("Previous experience cannot have more than 4000 characters.");

            byte[] picture = null;
            if (changePicture && pictureStream != null)
                picture = AdvisorBusiness.GetPictureBytes(pictureStream, pictureExtension);

            RequestToBeAdvisor request = null;
            User user = null;
            if (LoggedEmail != null)
            {
                if (!string.IsNullOrWhiteSpace(email) && email != LoggedEmail)
                    throw new BusinessException("Invalid email.");
                if (!string.IsNullOrWhiteSpace(password))
                    throw new BusinessException("Invalid password.");

                user = UserBusiness.GetByEmail(LoggedEmail);
                if (user == null)
                    throw new NotFoundException("User not found.");
                if (user.IsAdvisor)
                    throw new BusinessException("User was already approved as advisor.");

                request = GetByUser(user.Id);
                if (request?.Approved == true)
                    throw new BusinessException("Request was already approved.");
            }
            else 
                user = UserBusiness.GetValidUserToRegister(email, password, null);

            Guid? urlGuid = null;
            if (picture != null)
            {
                urlGuid = Guid.NewGuid();
                if (await AzureStorageBusiness.UploadUserPictureFromBytesAsync($"{urlGuid}.png", picture) && request != null && request.UrlGuid.HasValue)
                    await AzureStorageBusiness.DeleteUserPicture($"{request.UrlGuid.Value}.png");
            }
            else if (!changePicture)
                urlGuid = request?.UrlGuid;

            RequestToBeAdvisor newRequest = null;
            using (var transaction = TransactionalDapperCommand)
            {
                if (LoggedEmail == null)
                    transaction.Insert(user);
                
                newRequest = new RequestToBeAdvisor()
                {
                    CreationDate = Data.GetDateTimeNow(),
                    Name = name,
                    Description = description,
                    PreviousExperience = previousExperience,
                    UserId = user.Id,
                    UrlGuid = urlGuid
                };
                transaction.Insert(newRequest);
                transaction.Commit();
            }
            if (LoggedEmail == null)
                await UserBusiness.SendEmailConfirmationAsync(user.Email, user.ConfirmationCode);

            await SendRequestToBeAdvisorEmailAsync(user, newRequest, request);
            return newRequest;
        }
        private async Task SendRequestToBeAdvisorEmailAsync(User user, RequestToBeAdvisor newRequestToBeAdvisor, RequestToBeAdvisor oldRequestToBeAdvisor)
        {
            await EmailBusiness.SendErrorEmailAsync(string.Format(@"Email: {0} 
<br/>
<br/>
<b>Old Name</b>: {1}
<br/>
<b>New Name</b>: {2}
<br/>
<br/>
<b>Old Description</b>: {3}
<br/>
<b>New Description</b>: {4}
<br/>
<br/>
<b>Old Previous Experience</b>: {5}
<br/>
<b>New Previous Experience</b>: {6}", user.Email, 
oldRequestToBeAdvisor?.Name ?? "N/A", newRequestToBeAdvisor.Name,
oldRequestToBeAdvisor?.Description ?? "N/A", newRequestToBeAdvisor.Description,
oldRequestToBeAdvisor?.PreviousExperience ?? "N/A", newRequestToBeAdvisor.PreviousExperience),
string.Format("[{0}] Request to be adivosr - Auctus Beta", oldRequestToBeAdvisor == null ? "NEW" : "UPDATE"));
        }

        private async Task SendRequestRejectedNotificationAsync(User user)
        {
            await EmailBusiness.SendAsync(new string[] { user.Email },
                "Your request to become an expert was rejected - Auctus Beta",
                $@"Hello,
<br/><br/>
We are sorry to inform you that at this moment your request to become an expert can not be accepted.
<br/><br/>
Thanks,
<br/>
Auctus Team");
        }

        private async Task SendRequestApprovedNotificationAsync(User user)
        {
            await EmailBusiness.SendAsync(new string[] { user.Email },
                "Your request to become an expert was approved! - Auctus Beta",
                string.Format($@"Hello,
<br/><br/>
We are happy to inform you that your request to become an Expert on Auctus Platform was approved. To start recommending assets now, <a href='{0}expert-details/{1}' target='_blank'>click here</a>.
<br/><br/>
Thanks,
<br/>
Auctus Team", WebUrl, user.Id));
        }
    }
}
