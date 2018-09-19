using Auctus.Business.Account;
using Auctus.Business.Advisor;
using Auctus.Business.Asset;
using Auctus.DataAccessInterfaces.Account;
using Auctus.DataAccessInterfaces.Advisor;
using Auctus.DataAccessInterfaces.Asset;
using Auctus.DataAccessInterfaces.Blockchain;
using Auctus.DataAccessInterfaces.Email;
using Auctus.DataAccessInterfaces.Exchange;
using Auctus.DataAccessInterfaces.Storage;
using Auctus.DataAccessMock.Account;
using Auctus.DataAccessMock.Advisor;
using Auctus.DataAccessMock.Asset;
using Auctus.DataAccessMock.Blockchain;
using Auctus.DataAccessMock.Email;
using Auctus.DataAccessMock.Exchange;
using Auctus.DataAccessMock.Storage;
using Auctus.DomainObjects.Account;
using Auctus.DomainObjects.Advisor;
using Auctus.DomainObjects.Asset;
using Auctus.Util;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Reflection;

namespace Auctus.Test
{
    public abstract class BaseTest : IDisposable
    {
        private readonly IServiceProvider ServiceProvider;
        private readonly IServiceScopeFactory ServiceScopeFactory;
        private readonly ILoggerFactory LoggerFactory;
        private readonly IConfigurationRoot Configuration;
        private readonly Cache MemoryCache;
        protected string LoggedEmail;
        private readonly string LoggedIp;

        protected BaseTest()
        {
            var rootPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "..\\..\\..\\..\\Api");
            var builder = new ConfigurationBuilder()
                .SetBasePath(rootPath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddEnvironmentVariables();
            Configuration = builder.Build();

            IServiceCollection services = new ServiceCollection();
            services.AddMemoryCache();
            services.AddSingleton<Cache>();

            services.AddSingleton<IEmailResource, EmailResource>();
            services.AddSingleton<IWeb3Api, Web3Api>();
            services.AddSingleton<IAzureStorageResource, AzureStorageResource>();
            services.AddSingleton<ICoinMarketcapApi, CoinMarketcapApi>();
            services.AddSingleton<ICoinGeckoApi, CoinGeckoApi>();
            services.AddScoped<IActionData<DomainObjects.Account.Action>, ActionData>();
            services.AddScoped<IExchangeApiAccessData<ExchangeApiAccess>, ExchangeApiAccessData>();
            services.AddScoped<IPasswordRecoveryData<PasswordRecovery>, PasswordRecoveryData>();
            services.AddScoped<IUserData<User>, UserData>();
            services.AddScoped<IWalletData<Wallet>, WalletData>();
            services.AddScoped<IAdviceData<Advice>, AdviceData>();
            services.AddScoped<IAdvisorData<DomainObjects.Advisor.Advisor>, AdvisorData>();
            services.AddScoped<IRequestToBeAdvisorData<RequestToBeAdvisor>, RequestToBeAdvisorData>();
            services.AddScoped<IAssetData<DomainObjects.Asset.Asset>, AssetData>();
            services.AddScoped<IAssetValueData<AssetValue>, AssetValueData>();
            services.AddScoped<IFollowAdvisorData<FollowAdvisor>, FollowAdvisorData>();
            services.AddScoped<IFollowAssetData<FollowAsset>, FollowAssetData>();
            services.AddScoped<IFollowData<Follow>, FollowData>();
            services.AddScoped<IAssetCurrentValueData<AssetCurrentValue>, AssetCurrentValueData>();

            ServiceProvider = services.BuildServiceProvider();
            ServiceScopeFactory = new ServiceScopeFactory(ServiceProvider);
            MemoryCache = ServiceProvider.GetRequiredService<Cache>();
            LoggerFactory = new LoggerFactory();
            LoggedEmail = "test@auctus.org";
            LoggedIp = "10.0.0.1";
        }

        public virtual void Dispose()
        {
        }

        private UserBusiness _userBusiness;
        private PasswordRecoveryBusiness _passwordRecoveryBusiness;
        private AdvisorBusiness _advisorBusiness;
        private AdviceBusiness _adviceBusiness;
        private AssetBusiness _assetBusiness;
        private AssetValueBusiness _assetValueBusiness;
        private FollowBusiness _followBusiness;
        private FollowAssetBusiness _followAssetBusiness;
        private FollowAdvisorBusiness _followAdvisorBusiness;
        private ExchangeApiAccessBusiness _exchangeApiAccessBusiness;
        private RequestToBeAdvisorBusiness _requestToBeAdvisorBusiness;
        private WalletBusiness _walletBusiness;
        private ActionBusiness _actionBusiness;
        private AssetCurrentValueBusiness _assetCurrentValueBusiness;

        protected UserBusiness UserBusiness
        {
            get
            {
                if (_userBusiness == null)
                    _userBusiness = new UserBusiness(Configuration, ServiceProvider, ServiceScopeFactory, LoggerFactory, MemoryCache, LoggedEmail, LoggedIp);
                return _userBusiness;
            }
        }

        protected PasswordRecoveryBusiness PasswordRecoveryBusiness
        {
            get
            {
                if (_passwordRecoveryBusiness == null)
                    _passwordRecoveryBusiness = new PasswordRecoveryBusiness(Configuration, ServiceProvider, ServiceScopeFactory, LoggerFactory, MemoryCache, LoggedEmail, LoggedIp);
                return _passwordRecoveryBusiness;
            }
        }

        protected AdvisorBusiness AdvisorBusiness
        {
            get
            {
                if (_advisorBusiness == null)
                    _advisorBusiness = new AdvisorBusiness(Configuration, ServiceProvider, ServiceScopeFactory, LoggerFactory, MemoryCache, LoggedEmail, LoggedIp);
                return _advisorBusiness;
            }
        }

        protected AdviceBusiness AdviceBusiness
        {
            get
            {
                if (_adviceBusiness == null)
                    _adviceBusiness = new AdviceBusiness(Configuration, ServiceProvider, ServiceScopeFactory, LoggerFactory, MemoryCache, LoggedEmail, LoggedIp);
                return _adviceBusiness;
            }
        }

        protected FollowBusiness FollowBusiness
        {
            get
            {
                if (_followBusiness == null)
                    _followBusiness = new FollowBusiness(Configuration, ServiceProvider, ServiceScopeFactory, LoggerFactory, MemoryCache, LoggedEmail, LoggedIp);
                return _followBusiness;
            }
        }

        protected FollowAssetBusiness FollowAssetBusiness
        {
            get
            {
                if (_followAssetBusiness == null)
                    _followAssetBusiness = new FollowAssetBusiness(Configuration, ServiceProvider, ServiceScopeFactory, LoggerFactory, MemoryCache, LoggedEmail, LoggedIp);
                return _followAssetBusiness;
            }
        }

        protected FollowAdvisorBusiness FollowAdvisorBusiness
        {
            get
            {
                if (_followAdvisorBusiness == null)
                    _followAdvisorBusiness = new FollowAdvisorBusiness(Configuration, ServiceProvider, ServiceScopeFactory, LoggerFactory, MemoryCache, LoggedEmail, LoggedIp);
                return _followAdvisorBusiness;
            }
        }

        protected AssetBusiness AssetBusiness
        {
            get
            {
                if (_assetBusiness == null)
                    _assetBusiness = new AssetBusiness(Configuration, ServiceProvider, ServiceScopeFactory, LoggerFactory, MemoryCache, LoggedEmail, LoggedIp);
                return _assetBusiness;
            }
        }

        protected AssetValueBusiness AssetValueBusiness
        {
            get
            {
                if (_assetValueBusiness == null)
                    _assetValueBusiness = new AssetValueBusiness(Configuration, ServiceProvider, ServiceScopeFactory, LoggerFactory, MemoryCache, LoggedEmail, LoggedIp);
                return _assetValueBusiness;
            }
        }

        protected ExchangeApiAccessBusiness ExchangeApiAccessBusiness
        {
            get
            {
                if (_exchangeApiAccessBusiness == null)
                    _exchangeApiAccessBusiness = new ExchangeApiAccessBusiness(Configuration, ServiceProvider, ServiceScopeFactory, LoggerFactory, MemoryCache, LoggedEmail, LoggedIp);
                return _exchangeApiAccessBusiness;
            }
        }

        protected RequestToBeAdvisorBusiness RequestToBeAdvisorBusiness
        {
            get
            {
                if (_requestToBeAdvisorBusiness == null)
                    _requestToBeAdvisorBusiness = new RequestToBeAdvisorBusiness(Configuration, ServiceProvider, ServiceScopeFactory, LoggerFactory, MemoryCache, LoggedEmail, LoggedIp);
                return _requestToBeAdvisorBusiness;
            }
        }

        protected WalletBusiness WalletBusiness
        {
            get
            {
                if (_walletBusiness == null)
                    _walletBusiness = new WalletBusiness(Configuration, ServiceProvider, ServiceScopeFactory, LoggerFactory, MemoryCache, LoggedEmail, LoggedIp);
                return _walletBusiness;
            }
        }

        protected ActionBusiness ActionBusiness
        {
            get
            {
                if (_actionBusiness == null)
                    _actionBusiness = new ActionBusiness(Configuration, ServiceProvider, ServiceScopeFactory, LoggerFactory, MemoryCache, LoggedEmail, LoggedIp);
                return _actionBusiness;
            }
        }

        protected AssetCurrentValueBusiness AssetCurrentValueBusiness
        {
            get
            {
                if (_assetCurrentValueBusiness == null)
                    _assetCurrentValueBusiness = new AssetCurrentValueBusiness(Configuration, ServiceProvider, ServiceScopeFactory, LoggerFactory, MemoryCache, LoggedEmail, LoggedIp);
                return _assetCurrentValueBusiness;
            }
        }
    }
}
