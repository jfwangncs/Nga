<template>
  <header class="app-header">
    <meta name="google-adsense-account" content="ca-pub-6574104985269682" />
    <div class="header-content">
      <div class="header-left">
        <router-link to="/" class="logo">NGB</router-link>
        <nav class="nav">
          <router-link to="/" class="nav-link" active-class="active"
            >全部主题</router-link
          >
        </nav>
      </div>
      <div class="header-right">
        <div class="user-avatar"></div>
        <span class="user-name">用户</span>
      </div>
    </div>
  </header>
</template>

<script setup></script>

<style scoped>
.app-header {
  width: 100%;
  height: 64px;
  background: #ffffff;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.06);
}

.header-content {
  max-width: 1440px;
  height: 100%;
  margin: 0 auto;
  padding: 0 32px;
  display: flex;
  align-items: center;
  justify-content: space-between;
}

.header-left {
  display: flex;
  align-items: center;
  gap: 12px;
}

.logo {
  font-size: 20px;
  font-weight: 700;
  color: #1a1a1a;
}

.nav {
  display: flex;
  gap: 24px;
  margin-left: 24px;
}

.nav-link {
  font-size: 14px;
  color: #666666;
  transition: color 0.2s;
}

.nav-link:hover,
.nav-link.active {
  color: #2196f3;
}

.header-right {
  display: flex;
  align-items: center;
  gap: 12px;
}

.user-avatar {
  width: 36px;
  height: 36px;
  border-radius: 50%;
  background: #e3f2fd;
}

.user-name {
  font-size: 14px;
  font-weight: 500;
  color: #333333;
}
</style>
