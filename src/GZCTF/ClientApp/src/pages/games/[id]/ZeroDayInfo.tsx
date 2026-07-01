import { Alert, Card, Center, Container, Group, Loader, Stack, Text, Title } from '@mantine/core'
import { mdiAlertCircleOutline, mdiBugCheck } from '@mdi/js'
import { Icon } from '@mdi/react'
import { FC, useMemo } from 'react'
import { useTranslation } from 'react-i18next'
import { useParams } from 'react-router'
import { Markdown } from '@Components/MarkdownRenderer'
import { WithGameTab } from '@Components/WithGameTab'
import { WithNavBar } from '@Components/WithNavbar'
import { useGame } from '@Hooks/useGame'
import { Api } from '@Api'

const BUGBOUNTY_URL = 'https://bugbounty.openharmony.cn/bugbounty/leak'

const ZeroDayInfo: FC = () => {
  const { t } = useTranslation()
  const { id } = useParams()
  const numId = Number(id)
  const { game } = useGame(numId)

  // 获取队伍信息 + 0day 文案 —— 通过 game detail API（含 zeroDayInfoMarkdown + teamToken）
  const { data: detail } = Api.Game.useGameChallengesWithTeamInfo(numId, {
    revalidateOnFocus: false,
  })

  const renderedMarkdown = useMemo(() => {
    const md = detail?.zeroDayInfoMarkdown ?? ''
    if (!md) return ''
    const teamToken = detail?.teamToken ?? ''
    const teamName = detail?.rank?.name ?? ''
    return md
      .replaceAll('{{teamToken}}', teamToken)
      .replaceAll('{{teamName}}', teamName)
      .replaceAll('{{bugbounty_url}}', BUGBOUNTY_URL)
  }, [detail?.zeroDayInfoMarkdown, detail?.teamToken, detail?.rank?.name])

  if (!game) {
    return (
      <WithNavBar>
        <Center h="50vh">
          <Loader />
        </Center>
      </WithNavBar>
    )
  }

  return (
    <WithNavBar>
      <WithGameTab>
        <Container size="lg">
          <Stack gap="md">
            <Card withBorder shadow="sm" p="lg">
              <Group justify="space-between" mb="md">
                <Group gap="sm">
                  <Icon path={mdiBugCheck} size={1.2} />
                  <Title order={3}>{t('game.zeroday_info.title')}</Title>
                </Group>
              </Group>

              {renderedMarkdown ? (
                <Markdown source={renderedMarkdown} />
              ) : (
                <Alert color="yellow" icon={<Icon path={mdiAlertCircleOutline} size={1} />}>
                  {t('game.zeroday_info.empty')}
                </Alert>
              )}
            </Card>

            <Card withBorder shadow="sm" p="lg">
              <Group justify="space-between">
                <Stack gap={0}>
                  <Text size="sm" c="dimmed">
                    {t('game.zeroday_info.platform_label')}
                  </Text>
                  <Text fw={700}>{BUGBOUNTY_URL}</Text>
                </Stack>
                <a href={BUGBOUNTY_URL} target="_blank" rel="noopener noreferrer">
                  <Text c="blue" fw={600}>
                    {t('game.zeroday_info.open_platform')} →
                  </Text>
                </a>
              </Group>
            </Card>
          </Stack>
        </Container>
      </WithGameTab>
    </WithNavBar>
  )
}

export default ZeroDayInfo
