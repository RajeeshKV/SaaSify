# SaaSify - UI Integration Guide

## Overview
This guide provides complete integration examples for connecting your frontend application to the SaaSify production-grade multi-tenant SaaS API.

---

## 🔐 **Authentication Integration**

### Login Request
```javascript
// POST /api/auth/login
const loginResponse = await fetch('/api/auth/login', {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({
    email: 'user@example.com',
    password: 'password123'
  })
});

const { token, refreshToken, tenantName } = await loginResponse.json();

// Store tokens
localStorage.setItem('accessToken', token);
localStorage.setItem('refreshToken', refreshToken);
localStorage.setItem('tenantName', tenantName);
```

### Login Response Structure
```json
{
  "tenantId": 1,
  "userId": 123,
  "email": "user@example.com",
  "role": "Admin",
  "token": "eyJhbGciOiJIUzI1NiIs...",
  "refreshToken": "abc123...",
  "accessTokenExpiresAt": "2026-05-02T12:00:00Z",
  "tenantName": "Acme Corporation"
}
```

### Authenticated API Calls
```javascript
// Add Authorization header to all requests
const apiCall = async (url, options = {}) => {
  const token = localStorage.getItem('accessToken');
  return fetch(url, {
    ...options,
    headers: {
      'Content-Type': 'application/json',
      'Authorization': `Bearer ${token}`,
      ...options.headers
    }
  });
};
```

---

## 🏢 **Tenant Management**

### Display Tenant Information
```javascript
// Get tenant info from login response or API
const tenantName = localStorage.getItem('tenantName') || 'Unknown';

// React Component
function TenantHeader() {
  return (
    <header>
      <h1>Welcome to {tenantName}</h1>
      <div className="tenant-info">
        <span>Organization: {tenantName}</span>
      </div>
    </header>
  );
}
```

### Tenant Settings
```javascript
// GET /api/tenant-settings
const settingsResponse = await apiCall('/api/tenant-settings');
const settings = await settingsResponse.json();

// Update settings
await apiCall('/api/tenant-settings', {
  method: 'PUT',
  body: JSON.stringify({
    maxProjects: 10,
    maxUsers: 5,
    enableAdvancedFeatures: true,
    enableApiAccess: true,
    enableExport: true,
    enableIntegrations: false,
    maxStorageMB: 1000,
    maxApiCallsPerDay: 1000
  })
});
```

---

## 💳 **Subscription & Billing with Stripe Integration**

### Get Current Subscription
```javascript
// GET /api/subscription/current
const subscriptionResponse = await apiCall('/api/subscription/current');
const subscription = await subscriptionResponse.json();

// Display subscription info
function SubscriptionInfo() {
  return (
    <div className="subscription-card">
      <h3>Current Plan: {subscription.plan}</h3>
      <p>Status: {subscription.isActive ? 'Active' : 'Inactive'}</p>
      <p>Valid until: {new Date(subscription.endDate).toLocaleDateString()}</p>
      <p>Monthly: ${subscription.amount}</p>
      <div className="subscription-actions">
        <button onClick={() => manageBilling()}>
          Manage Billing
        </button>
        <button onClick={() => cancelSubscription()}>
          Cancel Subscription
        </button>
      </div>
    </div>
  );
}
```

### Create Stripe Checkout Session
```javascript
// POST /api/stripe/create-checkout-session
const createCheckoutSession = async (planId) => {
  try {
    const response = await apiCall('/api/stripe/create-checkout-session', {
      method: 'POST',
      body: JSON.stringify({ planId })
    });
    
    const { checkoutUrl } = await response.json();
    
    // Redirect to Stripe checkout
    window.location.href = checkoutUrl;
  } catch (error) {
    console.error('Failed to create checkout session:', error);
    // Show error to user
    alert('Unable to process upgrade. Please try again.');
  }
};
```

### Available Plans with Stripe Integration
```javascript
// GET /api/subscription/plans
const plansResponse = await apiCall('/api/subscription/plans');
const plans = await plansResponse.json();

// Render pricing cards with Stripe checkout
function PricingPlans() {
  return (
    <div className="pricing-grid">
      {plans.map(plan => (
        <div key={plan.name} className="plan-card">
          <div className="plan-header">
            <h3>{plan.name}</h3>
            <div className="price">
              <span className="currency">$</span>
              <span className="amount">{plan.monthlyPrice}</span>
              <span className="period">/month</span>
            </div>
          </div>
          
          <p className="description">{plan.description}</p>
          
          <div className="features">
            <h4>Features:</h4>
            <ul>
              {plan.features.map(feature => (
                <li key={feature}>
                  <span className="checkmark">✓</span>
                  {feature}
                </li>
              ))}
            </ul>
          </div>
          
          <div className="plan-actions">
            <button 
              className={`btn ${plan.name === 'Free' ? 'btn-secondary' : 'btn-primary'}`}
              onClick={() => plan.name === 'Free' ? selectPlan(plan.name) : createCheckoutSession(plan.name)}
              disabled={currentPlan === plan.name}
            >
              {plan.name === 'Free' ? 'Current Plan' : 
               currentPlan === plan.name ? 'Current Plan' : 
               `Upgrade to ${plan.name}`}
            </button>
          </div>
        </div>
      ))}
    </div>
  );
}

// Usage
const [currentPlan, setCurrentPlan] = useState('Free');

useEffect(() => {
  // Load current subscription
  loadCurrentSubscription();
}, []);

const loadCurrentSubscription = async () => {
  try {
    const response = await apiCall('/api/subscription/current');
    const subscription = await response.json();
    setCurrentPlan(subscription.plan);
  } catch (error) {
    console.error('Failed to load subscription:', error);
  }
};
```

### Billing Management
```javascript
// Billing management component
function BillingManagement() {
  const [subscription, setSubscription] = useState(null);
  const [loading, setLoading] = useState(false);

  const handleUpgrade = async (planId) => {
    setLoading(true);
    try {
      await createCheckoutSession(planId);
    } catch (error) {
      setLoading(false);
    }
  };

  const handleCancel = async () => {
    if (!confirm('Are you sure you want to cancel your subscription?')) return;
    
    try {
      // Redirect to Stripe customer portal
      const response = await apiCall('/api/stripe/customer-portal');
      const { portalUrl } = await response.json();
      window.location.href = portalUrl;
    } catch (error) {
      console.error('Failed to open customer portal:', error);
    }
  };

  return (
    <div className="billing-management">
      <h2>Billing & Subscription</h2>
      
      {subscription && (
        <div className="current-subscription">
          <h3>Current Subscription</h3>
          <div className="subscription-details">
            <p><strong>Plan:</strong> {subscription.plan}</p>
            <p><strong>Status:</strong> 
              <span className={`status ${subscription.isActive ? 'active' : 'inactive'}`}>
                {subscription.isActive ? 'Active' : 'Inactive'}
              </span>
            </p>
            <p><strong>Next Billing:</strong> 
              {new Date(subscription.endDate).toLocaleDateString()}
            </p>
            <p><strong>Amount:</strong> ${subscription.amount}/month</p>
          </div>
          
          <div className="billing-actions">
            <button onClick={handleUpgrade} className="btn btn-primary">
              Upgrade Plan
            </button>
            <button onClick={handleCancel} className="btn btn-secondary">
              Manage Billing
            </button>
          </div>
        </div>
      )}
      
      <div className="payment-methods">
        <h3>Payment Methods</h3>
        <button onClick={() => window.location.href = '/api/stripe/customer-portal'}>
          Manage Payment Methods
        </button>
      </div>
      
      <div className="billing-history">
        <h3>Billing History</h3>
        <button onClick={() => window.location.href = '/api/stripe/customer-portal'}>
          View Invoices
        </button>
      </div>
    </div>
  );
}
```

### Payment Success/Error Handling
```javascript
// Handle payment success and error pages
function usePaymentStatus() {
  const [status, setStatus] = useState(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const urlParams = new URLSearchParams(window.location.search);
    const sessionId = urlParams.get('session_id');
    const success = urlParams.get('success');
    const canceled = urlParams.get('canceled');

    if (success === 'true') {
      setStatus('success');
      setLoading(false);
    } else if (canceled === 'true') {
      setStatus('canceled');
      setLoading(false);
    } else if (sessionId) {
      // Verify session status with backend
      verifyPaymentStatus(sessionId);
    } else {
      setLoading(false);
    }
  }, []);

  const verifyPaymentStatus = async (sessionId) => {
    try {
      const response = await apiCall(`/api/stripe/verify-session?session_id=${sessionId}`);
      const sessionStatus = await response.json();
      setStatus(sessionStatus.status);
    } catch (error) {
      setStatus('error');
    } finally {
      setLoading(false);
    }
  };

  return { status, loading };
}

// Usage in component
function PaymentStatusPage() {
  const { status, loading } = usePaymentStatus();

  if (loading) {
    return <div className="loading">Verifying payment status...</div>;
  }

  switch (status) {
    case 'success':
      return (
        <div className="payment-success">
          <h2>🎉 Payment Successful!</h2>
          <p>Your subscription has been upgraded successfully.</p>
          <button onClick={() => window.location.href = '/dashboard'}>
            Go to Dashboard
          </button>
        </div>
      );
      
    case 'canceled':
      return (
        <div className="payment-canceled">
          <h2>Payment Canceled</h2>
          <p>Your payment was canceled. No charges were made.</p>
          <button onClick={() => window.location.href = '/billing'}>
            Return to Billing
          </button>
        </div>
      );
      
    case 'error':
      return (
        <div className="payment-error">
          <h2>Payment Error</h2>
          <p>There was an issue processing your payment. Please try again.</p>
          <button onClick={() => window.location.href = '/billing'}>
            Return to Billing
          </button>
        </div>
      );
      
    default:
      return <div className="payment-unknown">Unknown payment status</div>;
  }
}
```

---

## 📊 **Projects with Pagination**

### Get Projects
```javascript
// GET /api/projects?pageNumber=1&pageSize=10
const getProjects = async (page = 1, pageSize = 10) => {
  const response = await apiCall(`/api/projects?pageNumber=${page}&pageSize=${pageSize}`);
  return response.json();
};

// Usage
const projects = await getProjects(1, 10);
```

### Projects Response Structure
```json
{
  "data": [
    {
      "id": 1,
      "name": "Project A",
      "description": "Description A",
      "createdAt": "2026-05-01T10:00:00Z"
    }
  ],
  "pageNumber": 1,
  "pageSize": 10,
  "totalItems": 45,
  "totalPages": 5,
  "hasPreviousPage": false,
  "hasNextPage": true
}
```

### Pagination Component
```javascript
function ProjectsList() {
  const [projects, setProjects] = useState(null);
  const [currentPage, setCurrentPage] = useState(1);
  const [pageSize, setPageSize] = useState(10);

  useEffect(() => {
    loadProjects(currentPage, pageSize);
  }, [currentPage, pageSize]);

  const loadProjects = async (page, size) => {
    const data = await getProjects(page, size);
    setProjects(data);
  };

  return (
    <div>
      <div className="projects-grid">
        {projects?.data.map(project => (
          <div key={project.id} className="project-card">
            <h3>{project.name}</h3>
            <p>{project.description}</p>
          </div>
        ))}
      </div>
      
      {projects && (
        <Pagination
          currentPage={projects.pageNumber}
          totalPages={projects.totalPages}
          hasPrevious={projects.hasPreviousPage}
          hasNext={projects.hasNextPage}
          onPageChange={setCurrentPage}
          onPageSizeChange={setPageSize}
        />
      )}
    </div>
  );
}
```

---

## � **RBAC Migration for Existing Tenants**

### One-Time Migration Setup
```javascript
// POST /api/rbac-migration/migrate
const migrateRBAC = async () => {
  try {
    const response = await apiCall('/api/rbac-migration/migrate', {
      method: 'POST'
    });
    
    const result = await response.json();
    
    if (result.success) {
      console.log('RBAC migration completed successfully');
      alert('RBAC migration completed. All users now have proper role assignments.');
    } else {
      console.error('RBAC migration failed:', result.message);
      alert('RBAC migration failed. Please contact support.');
    }
  } catch (error) {
    console.error('RBAC migration error:', error);
    alert('Unable to complete RBAC migration. Please try again.');
  }
};

// Check migration status
const checkMigrationStatus = async () => {
  try {
    const response = await apiCall('/api/rbac-migration/status');
    const status = await response.json();
    
    console.log('Migration Status:', status.status);
    return status.status;
  } catch (error) {
    console.error('Failed to check migration status:', error);
    return [];
  }
};
```

### Migration Status Component
```javascript
function RBACMigrationStatus() {
  const [status, setStatus] = useState([]);
  const [loading, setLoading] = useState(false);
  const [hasPermission, setHasPermission] = useState(false);

  useEffect(() => {
    // Check if user has tenant.admin permission
    const permissions = getUserPermissions();
    setHasPermission(permissions.includes('tenant.admin'));
    
    if (hasPermission) {
      loadMigrationStatus();
    }
  }, [hasPermission]);

  const loadMigrationStatus = async () => {
    setLoading(true);
    try {
      const migrationStatus = await checkMigrationStatus();
      setStatus(migrationStatus);
    } catch (error) {
      console.error('Failed to load migration status:', error);
    } finally {
      setLoading(false);
    }
  };

  const handleMigrate = async () => {
    if (!confirm('This will migrate all existing tenants to use the new RBAC system. Continue?')) {
      return;
    }
    
    setLoading(true);
    try {
      await migrateRBAC();
      await loadMigrationStatus(); // Refresh status
    } catch (error) {
      console.error('Migration failed:', error);
    } finally {
      setLoading(false);
    }
  };

  if (!hasPermission) {
    return null; // Only show to tenant admins
  }

  return (
    <div className="rbac-migration">
      <h3>RBAC Migration Status</h3>
      
      {loading ? (
        <div className="loading">Checking migration status...</div>
      ) : (
        <>
          <div className="migration-status">
            <h4>Current Status:</h4>
            <ul>
              {status.map((item, index) => (
                <li key={index} className={item.includes('Migrated') ? 'migrated' : 'not-migrated'}>
                  {item}
                </li>
              ))}
            </ul>
          </div>
          
          {status.some(item => item.includes('Not Migrated')) && (
            <div className="migration-actions">
              <button 
                onClick={handleMigrate}
                className="btn btn-primary"
                disabled={loading}
              >
                {loading ? 'Migrating...' : 'Migrate All Tenants'}
              </button>
              <p className="warning">
                ⚠️ This is a one-time migration that will assign proper RBAC roles to all existing users.
              </p>
            </div>
          )}
          
          {status.every(item => item.includes('Migrated')) && (
            <div className="migration-complete">
              <div className="success-message">
                ✅ All tenants have been migrated to the new RBAC system.
              </div>
            </div>
          )}
        </>
      )}
    </div>
  );
}
```

### Permission-Based UI with RBAC
```javascript
// Enhanced permission checking with RBAC
function useRBACPermissions() {
  const [permissions, setPermissions] = useState([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const loadPermissions = () => {
      const token = localStorage.getItem('accessToken');
      if (token) {
        const payload = JSON.parse(atob(token.split('.')[1]));
        setPermissions(payload.permission || []);
      }
      setLoading(false);
    };

    loadPermissions();
  }, []);

  const hasPermission = (permission) => {
    return permissions.includes(permission);
  };

  const hasAnyPermission = (permissionList) => {
    return permissionList.some(permission => permissions.includes(permission));
  };

  const isAdmin = () => {
    return hasPermission('tenant.admin');
  };

  const canManageUsers = () => {
    return hasPermission('user.manage');
  };

  const canManageProjects = () => {
    return hasAnyPermission(['project.read', 'project.write', 'project.delete']);
  };

  return {
    permissions,
    loading,
    hasPermission,
    hasAnyPermission,
    isAdmin,
    canManageUsers,
    canManageProjects
  };
}

// Usage in components
function AdminPanel() {
  const { isAdmin, loading } = useRBACPermissions();

  if (loading) {
    return <div>Loading permissions...</div>;
  }

  if (!isAdmin) {
    return (
      <div className="access-denied">
        <h2>Access Denied</h2>
        <p>You need tenant administrator permissions to access this panel.</p>
      </div>
    );
  }

  return (
    <div className="admin-panel">
      <h2>Tenant Administration</h2>
      
      <div className="admin-sections">
        <RBACMigrationStatus />
        
        <div className="user-management">
          <h3>User Management</h3>
          {/* User management UI */}
        </div>
        
        <div className="role-management">
          <h3>Role Management</h3>
          {/* Role management UI */}
        </div>
        
        <div className="tenant-settings">
          <h3>Tenant Settings</h3>
          {/* Tenant settings UI */}
        </div>
      </div>
    </div>
  );
}
```

### Role Display in User Interface
```javascript
// Display user roles and permissions
function UserProfile() {
  const { permissions, loading } = useRBACPermissions();
  const [userRole, setUserRole] = useState('');

  useEffect(() => {
    // Derive user role from permissions
    if (permissions.includes('tenant.admin')) {
      setUserRole('Admin');
    } else if (permissions.includes('user.manage')) {
      setUserRole('Manager');
    } else if (permissions.includes('project.read')) {
      setUserRole('User');
    } else {
      setUserRole('Guest');
    }
  }, [permissions]);

  if (loading) {
    return <div>Loading user profile...</div>;
  }

  return (
    <div className="user-profile">
      <div className="user-info">
        <h3>User Profile</h3>
        <div className="role-badge">
          <span className={`role ${userRole.toLowerCase()}`}>
            {userRole}
          </span>
        </div>
      </div>
      
      <div className="permissions">
        <h4>Your Permissions:</h4>
        <div className="permission-grid">
          {permissions.map(permission => (
            <div key={permission} className="permission-item">
              <span className="permission-icon">✓</span>
              <span className="permission-name">
                {permission.replace('.', ' ').replace(/\b\w/g, l => l.toUpperCase())}
              </span>
            </div>
          ))}
        </div>
      </div>
    </div>
  );
}
```

---

## �🔒 **Role-Based Access Control (RBAC)**

### Check User Permissions
```javascript
// Get permissions from JWT token
const getUserPermissions = () => {
  const token = localStorage.getItem('accessToken');
  const payload = JSON.parse(atob(token.split('.')[1]));
  return payload.permission || [];
};

// Permission-based rendering
function AdminPanel() {
  const permissions = getUserPermissions();
  
  if (!permissions.includes('tenant.admin')) {
    return <div>Access denied</div>;
  }

  return (
    <div className="admin-panel">
      <h2>Tenant Administration</h2>
      {/* Admin functionality */}
    </div>
  );
}

// Permission-based API calls
const canManageUsers = getUserPermissions().includes('user.manage');

if (canManageUsers) {
  // Show user management UI
}
```

### Permission Examples
- `project.read` - View projects
- `project.write` - Create/edit projects  
- `project.delete` - Delete projects
- `user.manage` - Manage users
- `subscription.manage` - Manage subscriptions
- `tenant.admin` - Full tenant administration

---

## 🎯 **Feature Flags & Limits**

### Check Feature Access
```javascript
// GET /api/tenant-settings/features/{feature}
const checkFeature = async (feature) => {
  const response = await apiCall(`/api/tenant-settings/features/${feature}`);
  return response.json(); // boolean
};

// Usage
const hasAdvancedFeatures = await checkFeature('advanced');
const hasApiAccess = await checkFeature('api');
const hasExport = await checkFeature('export');
```

### Check Resource Limits
```javascript
// GET /api/tenant-settings/limits/{resource}?currentUsage=5
const checkLimit = async (resource, currentUsage) => {
  const response = await apiCall(`/api/tenant-settings/limits/${resource}?currentUsage=${currentUsage}`);
  return response.json(); // boolean
};

// Usage
const canCreateProject = await checkLimit('projects', currentProjectCount);
const canAddUser = await checkLimit('users', currentUserCount);
```

### Feature-Based UI
```javascript
function FeatureBasedUI() {
  const [features, setFeatures] = useState({});

  useEffect(() => {
    const loadFeatures = async () => {
      const [advanced, api, export, integrations] = await Promise.all([
        checkFeature('advanced'),
        checkFeature('api'),
        checkFeature('export'),
        checkFeature('integrations')
      ]);
      
      setFeatures({
        advanced,
        api,
        export,
        integrations
      });
    };
    
    loadFeatures();
  }, []);

  return (
    <div>
      {features.advanced && (
        <div className="advanced-features">
          {/* Advanced features UI */}
        </div>
      )}
      
      {features.api && (
        <div className="api-section">
          {/* API access UI */}
        </div>
      )}
      
      {features.export && (
        <button className="export-btn">
          Export Data
        </button>
      )}
      
      {features.integrations && (
        <div className="integrations">
          {/* Integration settings */}
        </div>
      )}
    </div>
  );
}
```

---

## 🚨 **Error Handling**

### API Error Handling
```javascript
const apiCall = async (url, options = {}) => {
  try {
    const token = localStorage.getItem('accessToken');
    const response = await fetch(url, {
      ...options,
      headers: {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${token}`,
        ...options.headers
      }
    });

    if (!response.ok) {
      const error = await response.json();
      
      // Handle specific error codes
      if (response.status === 403 && error.code === 'TENANT_INACTIVE') {
        // Redirect to billing page
        window.location.href = '/billing';
        return;
      }
      
      if (response.status === 401) {
        // Token expired, refresh or redirect to login
        await refreshToken();
        return apiCall(url, options); // Retry once
      }
      
      throw new Error(error.message || 'API request failed');
    }

    return response.json();
  } catch (error) {
    console.error('API Error:', error);
    throw error;
  }
};
```

### Error Display Component
```javascript
function ErrorDisplay({ error, onRetry }) {
  return (
    <div className="error-container">
      <div className="error-icon">⚠️</div>
      <h3>Something went wrong</h3>
      <p>{error.message}</p>
      
      {error.code === 'TENANT_INACTIVE' && (
        <div className="billing-prompt">
          <p>Your tenant account is inactive.</p>
          <button onClick={() => window.location.href = '/billing'}>
            Manage Subscription
          </button>
        </div>
      )}
      
      {onRetry && (
        <button onClick={onRetry} className="retry-btn">
          Try Again
        </button>
      )}
    </div>
  );
}
```

---

## 🎨 **Complete Dashboard Example**

```javascript
function Dashboard() {
  const [user, setUser] = useState(null);
  const [tenant, setTenant] = useState(null);
  const [subscription, setSubscription] = useState(null);
  const [projects, setProjects] = useState(null);
  const [settings, setSettings] = useState(null);

  useEffect(() => {
    loadDashboardData();
  }, []);

  const loadDashboardData = async () => {
    try {
      const [subData, projectsData, settingsData] = await Promise.all([
        apiCall('/api/subscription/current'),
        apiCall('/api/projects?pageNumber=1&pageSize=5'),
        apiCall('/api/tenant-settings')
      ]);

      setSubscription(subData);
      setProjects(projectsData);
      setSettings(settingsData);
    } catch (error) {
      console.error('Failed to load dashboard:', error);
    }
  };

  return (
    <div className="dashboard">
      <header>
        <h1>{tenant?.name} Dashboard</h1>
        <div className="user-info">
          <span>{user?.email}</span>
          <span>Plan: {subscription?.plan}</span>
        </div>
      </header>

      <main>
        <section className="overview">
          <div className="stats">
            <div className="stat-card">
              <h3>Projects</h3>
              <p>{projects?.totalItems || 0}</p>
              <small>of {settings?.maxProjects || '∞'}</small>
            </div>
            
            <div className="stat-card">
              <h3>Storage</h3>
              <p>Used / {settings?.maxStorageMB || '∞'} MB</p>
            </div>
            
            <div className="stat-card">
              <h3>API Calls</h3>
              <p>Today / {settings?.maxApiCallsPerDay || '∞'}</p>
            </div>
          </div>
        </section>

        <section className="projects">
          <h2>Recent Projects</h2>
          <ProjectsList projects={projects?.data || []} />
          
          {projects?.hasNextPage && (
            <button onClick={() => window.location.href = '/projects'}>
              View All Projects
            </button>
          )}
        </section>

        {settings?.enableAdvancedFeatures && (
          <section className="advanced-features">
            <h2>Advanced Features</h2>
            {/* Advanced features UI */}
          </section>
        )}
      </main>
    </div>
  );
}
```

---

## � **Environment Variables Configuration**

### Required Environment Variables

#### **Core Application Settings**
```bash
# Application Environment
ASPNETCORE_ENVIRONMENT=Production

# Database Configuration
DATABASE_URL=postgresql://username:password@host:port/database
# OR for Render: postgresql://user:pass@host:port/dbname?sslmode=require

# JWT Authentication Settings
JwtSettings__SecretKey=your-super-secret-jwt-key-min-256-bits
JwtSettings__Issuer=saasify-api
JwtSettings__Audience=saasify-client
JwtSettings__ExpiryMinutes=60
JwtSettings__RefreshTokenExpiryDays=7
```

#### **Stripe Billing Integration (Free Tier)**
```bash
# Stripe API Keys (Get from https://dashboard.stripe.com/apikeys)
Stripe__PublishableKey=pk_test_51234567890abcdef...
Stripe__SecretKey=sk_test_51234567890abcdef...
Stripe__WebhookSecret=whsec_51234567890abcdef...

# Stripe Configuration
Stripe__BaseUrl=https://your-api-domain.com
```

#### **Optional Configuration**
```bash
# Logging Level
Logging__LogLevel__Default=Information
Logging__LogLevel__Microsoft=Warning
Logging__LogLevel__Microsoft.Hosting.Lifetime=Information

# CORS Settings (if needed)
AllowedHosts=*
CORS__AllowedOrigins=https://your-frontend-domain.com,https://localhost:3000

# Rate Limiting (optional overrides)
RateLimiting__DefaultPermitLimit=100
RateLimiting__DefaultWindow=00:01:00
```

### Environment Setup by Platform

#### **Render Deployment**
```bash
# Set these in Render Dashboard > Environment
ASPNETCORE_ENVIRONMENT=Production
DATABASE_URL=postgresql://user:pass@host:port/dbname?sslmode=require
JwtSettings__SecretKey=your-super-secret-jwt-key-min-256-bits
JwtSettings__Issuer=saasify-api
JwtSettings__Audience=saasify-client
JwtSettings__ExpiryMinutes=60
JwtSettings__RefreshTokenExpiryDays=7
Stripe__PublishableKey=pk_test_51234567890abcdef...
Stripe__SecretKey=sk_test_51234567890abcdef...
Stripe__WebhookSecret=whsec_51234567890abcdef...
Stripe__BaseUrl=https://your-app-name.onrender.com
```

#### **Local Development (.env file)**
```bash
# .env file in project root
ASPNETCORE_ENVIRONMENT=Development
DATABASE_URL=Host=localhost;Database=saasify_dev;Username=postgres;Password=password
JwtSettings__SecretKey=dev-secret-key-for-local-development-only
JwtSettings__Issuer=saasify-api-dev
JwtSettings__Audience=saasify-client-dev
JwtSettings__ExpiryMinutes=1440
JwtSettings__RefreshTokenExpiryDays=30
Stripe__PublishableKey=pk_test_51234567890abcdef...
Stripe__SecretKey=sk_test_51234567890abcdef...
Stripe__WebhookSecret=whsec_51234567890abcdef...
Stripe__BaseUrl=https://localhost:7001
```

#### **Docker Environment**
```dockerfile
# In Dockerfile or docker-compose.yml
ENV ASPNETCORE_ENVIRONMENT=Production
ENV DATABASE_URL=postgresql://...
ENV JwtSettings__SecretKey=your-super-secret-jwt-key
ENV JwtSettings__Issuer=saasify-api
ENV JwtSettings__Audience=saasify-client
ENV Stripe__PublishableKey=pk_test_...
ENV Stripe__SecretKey=sk_test_...
ENV Stripe__WebhookSecret=whsec_...
```

### Stripe Setup Instructions

#### **1. Create Stripe Account**
1. Go to [Stripe Dashboard](https://dashboard.stripe.com)
2. Sign up for a free account
3. Verify your email and complete onboarding

#### **2. Get API Keys**
1. In Stripe Dashboard → Developers → API Keys
2. Copy the **Publishable Key** (starts with `pk_test_`)
3. Reveal and copy the **Secret Key** (starts with `sk_test_`)

#### **3. Configure Webhooks**
1. In Stripe Dashboard → Developers → Webhooks
2. Click "Add endpoint"
3. Set endpoint URL: `https://your-domain.com/api/stripe/webhook`
4. Select events to listen for:
   - `checkout.session.completed`
   - `invoice.payment_succeeded`
   - `customer.subscription.deleted`
5. Copy the **Signing Secret** (starts with `whsec_`)

#### **4. Test Configuration**
```javascript
// Test Stripe integration
const testStripeIntegration = async () => {
  try {
    const response = await fetch('/api/stripe/test-webhook', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ test: true })
    });
    
    const result = await response.json();
    console.log('Stripe integration test:', result);
  } catch (error) {
    console.error('Stripe test failed:', error);
  }
};
```

### Security Best Practices

#### **JWT Secret Key**
```bash
# Generate strong JWT secret (256 bits minimum)
openssl rand -base64 32
# OR use: dotnet user-secrets set JwtSettings:SecretKey "your-generated-key"
```

#### **Environment Security**
```bash
# Never commit secrets to git
echo ".env" >> .gitignore
echo "appsettings.Production.json" >> .gitignore

# Use platform-specific secret management
# Render: Use Environment Variables in dashboard
# Azure: Use Azure Key Vault
# AWS: Use AWS Secrets Manager
```

#### **Stripe Security**
```bash
# Use test keys for development
# Never use live keys in development
# Regularly rotate webhook secrets
# Monitor webhook delivery logs in Stripe Dashboard
```

### Configuration Validation

#### **Startup Validation**
```javascript
// Validate configuration on app startup
const validateConfig = () => {
  const required = [
    'JwtSettings__SecretKey',
    'DATABASE_URL',
    'Stripe__PublishableKey',
    'Stripe__SecretKey'
  ];
  
  const missing = required.filter(key => !process.env[key]);
  
  if (missing.length > 0) {
    throw new Error(`Missing required environment variables: ${missing.join(', ')}`);
  }
  
  console.log('✅ Configuration validated successfully');
};
```

#### **Health Check Configuration**
```javascript
// Check configuration health
const checkConfigHealth = async () => {
  try {
    const response = await fetch('/health/config');
    const health = await response.json();
    
    if (!health.healthy) {
      console.error('Configuration health check failed:', health.errors);
    }
    
    return health.healthy;
  } catch (error) {
    console.error('Failed to check configuration health:', error);
    return false;
  }
};
```

---

## 🚀 **Deployment Notes**

### Render Deployment
1. Connect your GitHub repository to Render
2. Use the provided `render.yaml` configuration
3. Set environment variables in Render dashboard
4. Deploy automatically on push to main branch

### Local Development
```bash
# Install dependencies
dotnet restore

# Run database migrations
dotnet ef database update

# Start the API
dotnet run --project WebAPI

# API will be available at: https://localhost:7001
# Swagger UI: https://localhost:7001/swagger
```

---

## 📞 **Support**

For any integration issues or questions:
- Check the API documentation at `/swagger`
- Review the error responses for specific error codes
- Ensure proper tenant context is set in all requests
- Verify JWT tokens are valid and not expired

The SaaSify API is designed to be developer-friendly with clear error messages and comprehensive REST endpoints for all multi-tenant SaaS functionality.
