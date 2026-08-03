<template>
  <div class="public-layout">
    <header class="public-header">
      <div class="header-content">
        <div class="logo">
          <span>⚡ G-Ops Hub</span>
        </div>
        <router-link to="/login" class="login-link">Admin Sign In</router-link>
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
}

.public-header {
  background: #1e293b;
  border-bottom: 1px solid rgba(255, 255, 255, 0.1);
  padding: 1rem 2rem;
}

.header-content {
  max-width: 1200px;
  margin: 0 auto;
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.logo {
  font-size: 1.25rem;
  font-weight: 800;
  color: #818cf8;
}

.login-link {
  color: #f8fafc;
  text-decoration: none;
  font-weight: 600;
  padding: 0.5rem 1rem;
  border-radius: 0.5rem;
  background: #6366f1;
  transition: background 0.2s ease;

  &:hover {
    background: #4f46e5;
  }
}

.public-body {
  flex: 1;
  max-width: 1200px;
  width: 100%;
  margin: 0 auto;
  padding: 2rem;
}
</style>
