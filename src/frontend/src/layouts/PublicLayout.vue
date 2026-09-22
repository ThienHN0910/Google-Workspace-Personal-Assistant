<template>
  <div class="public-layout">
    <header class="public-header">
      <div class="header-content">
        <div class="logo-group">
          <div class="logo-badge">
            <i class="pi pi-bolt"></i>
          </div>
          <div class="logo-info">
            <span class="logo-title">G-Ops Hub</span>
            <span class="logo-tag">Public Portal</span>
          </div>
        </div>
        <router-link to="/login" class="login-link">
          <i class="pi pi-lock"></i>
          <span>Admin Sign In</span>
        </router-link>
      </div>
    </header>
    <main class="public-body">
      <router-view />
    </main>
  </div>
</template>

<style scoped lang="scss">
.public-layout {
  min-height: 100vh;
  display: flex;
  flex-direction: column;
  background-color: #070b14;
  background-image: 
    radial-gradient(circle at 50% 0%, rgba(99, 102, 241, 0.12) 0%, transparent 65%),
    radial-gradient(circle at 100% 100%, rgba(6, 182, 212, 0.05) 0%, transparent 40%);
  color: #f1f5f9;
}

.public-header {
  background: rgba(11, 17, 32, 0.85);
  backdrop-filter: blur(16px);
  -webkit-backdrop-filter: blur(16px);
  border-bottom: 1px solid rgba(255, 255, 255, 0.08);
  padding: 0.85rem 1.5rem;
  position: sticky;
  top: 0;
  z-index: 100;
}

.header-content {
  max-width: 1280px;
  margin: 0 auto;
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.logo-group {
  display: flex;
  align-items: center;
  gap: 0.75rem;

  .logo-badge {
    width: 36px;
    height: 36px;
    border-radius: 0.6rem;
    background: linear-gradient(135deg, rgba(99, 102, 241, 0.3) 0%, rgba(6, 182, 212, 0.2) 100%);
    border: 1px solid rgba(99, 102, 241, 0.4);
    display: flex;
    align-items: center;
    justify-content: center;
    color: #38bdf8;
    box-shadow: 0 0 16px rgba(56, 189, 248, 0.25);
  }

  .logo-info {
    display: flex;
    align-items: center;
    gap: 0.5rem;

    .logo-title {
      font-size: 1.15rem;
      font-weight: 800;
      letter-spacing: -0.02em;
      background: linear-gradient(135deg, #f8fafc 0%, #cbd5e1 100%);
      -webkit-background-clip: text;
      -webkit-text-fill-color: transparent;
    }

    .logo-tag {
      font-size: 0.675rem;
      font-weight: 700;
      text-transform: uppercase;
      padding: 0.15rem 0.5rem;
      border-radius: 9999px;
      background: rgba(99, 102, 241, 0.15);
      border: 1px solid rgba(99, 102, 241, 0.3);
      color: #a5b4fc;
      letter-spacing: 0.04em;
    }
  }
}

.login-link {
  color: #f8fafc;
  text-decoration: none;
  font-weight: 600;
  font-size: 0.825rem;
  padding: 0.5rem 1rem;
  border-radius: 0.5rem;
  background: linear-gradient(135deg, #6366f1 0%, #4f46e5 100%);
  border: 1px solid rgba(99, 102, 241, 0.4);
  display: inline-flex;
  align-items: center;
  gap: 0.45rem;
  transition: all 0.15s ease;
  box-shadow: 0 2px 10px rgba(99, 102, 241, 0.25);

  &:hover {
    filter: brightness(1.1);
    transform: translate3d(0, -1px, 0);
    box-shadow: 0 4px 16px rgba(99, 102, 241, 0.4);
  }
}

.public-body {
  flex: 1;
  max-width: 1280px;
  width: 100%;
  margin: 0 auto;
  padding: 1.75rem 1.5rem 3rem;

  @media (max-width: 640px) {
    padding: 1rem 0.75rem 2rem;
  }
}
</style>
